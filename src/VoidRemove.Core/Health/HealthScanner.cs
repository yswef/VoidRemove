using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VoidRemove.Core.History;

namespace VoidRemove.Core.Health;

/// <summary>The toggles for a health scan.</summary>
public sealed record HealthScanOptions
{
    public int RedundantOlderThanDays { get; init; }

    public bool UseOnlineLookup { get; init; }
}

/// <summary>
/// Aggregates the health categories. Disk-based categories are computed on Windows; the
/// uninstall-issues category works everywhere from the local history.
/// </summary>
public sealed class HealthScanner
{
    private readonly IUninstallHistoryStore _history;
    private readonly IVirusTotalClient _virusTotal;
    private readonly ILogger<HealthScanner>? _logger;

    public HealthScanner(IUninstallHistoryStore history, IVirusTotalClient virusTotal, ILogger<HealthScanner>? logger = null)
    {
        _history = history;
        _virusTotal = virusTotal;
        _logger = logger;
    }

    public bool IsSupported => System.OperatingSystem.IsWindows();

    public async Task<HealthReport> ScanAsync(HealthScanOptions options, CancellationToken cancellationToken = default)
    {
        HealthReport report = new() { ScannedUtc = DateTime.UtcNow };

        await AddUninstallIssuesAsync(report, cancellationToken);

        if (IsSupported)
        {
            AddDiskCategories(report, options);
            AddPermissionFindings(report);
            AddNotificationFindings(report);
            await AddMalwareFindingsAsync(report, options, cancellationToken);
        }

        return report;
    }

    // ------------------------------------------------------- categories

    private async Task AddUninstallIssuesAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (UninstallReport entry in await _history.ListAsync(cancellationToken))
        {
            if (!entry.Succeeded && !entry.WasDryRun)
            {
                report.Findings.Add(new HealthFinding
                {
                    Category = HealthCategory.UninstallIssues,
                    Target = entry.ProgramName,
                    DisplayName = entry.ProgramName,
                    Detail = "The last removal attempt did not complete.",
                });
            }
        }
    }

    private void AddDiskCategories(HealthReport report, HealthScanOptions options)
    {
        List<FileEntry> entries = EnumerateDownloadLikeFiles();

        foreach (FileEntry file in RedundantFileDetector.Detect(entries.Where(static e => InTemp(e.Path)), options.RedundantOlderThanDays))
        {
            report.Findings.Add(Finding(HealthCategory.RedundantFiles, file));
        }

        foreach (FileEntry file in RedundantFileDetector.Detect(entries.Where(static e => !InTemp(e.Path)), options.RedundantOlderThanDays))
        {
            report.Findings.Add(Finding(HealthCategory.InstallationFiles, file));
        }
    }

    private static HealthFinding Finding(HealthCategory category, FileEntry file) => new()
    {
        Category = category,
        Target = file.Path,
        DisplayName = Path.GetFileName(file.Path),
        SizeBytes = file.SizeBytes,
        Detail = "Leftover installation artefact.",
    };

    private static bool InTemp(string path) =>
        path.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);

    private static List<FileEntry> EnumerateDownloadLikeFiles()
    {
        List<FileEntry> entries = new();

        foreach (string root in DownloadRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            try
            {
                foreach (string file in Directory.EnumerateFiles(root))
                {
                    FileInfo info = new(file);
                    entries.Add(new FileEntry(file, info.Length, info.LastWriteTimeUtc));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return entries;
    }

    private static IEnumerable<string> DownloadRoots()
    {
        yield return Path.GetTempPath();
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private void AddPermissionFindings(HealthReport report)
    {
        using RegistryKey? consent = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore");
        if (consent is null)
        {
            return;
        }

        foreach (string permission in consent.GetSubKeyNames())
        {
            int granted = CountGranted(consent, permission);
            if (granted > 0)
            {
                report.Findings.Add(new HealthFinding
                {
                    Category = HealthCategory.Permissions,
                    Target = permission,
                    DisplayName = permission,
                    Detail = $"{granted} app(s) currently hold this permission.",
                });
            }
        }
    }

    private static int CountGranted(RegistryKey consent, string permission)
    {
        int count = 0;
        foreach (string scope in new[] { "App", "NonPackaged" })
        {
            using RegistryKey? group = consent.OpenSubKey($"{permission}\\{scope}");
            if (group is null)
            {
                continue;
            }

            count += group.GetSubKeyNames().Length;
        }

        return count;
    }

    private void AddNotificationFindings(HealthReport report)
    {
        using RegistryKey? settings = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings");
        if (settings is null)
        {
            return;
        }

        foreach (string appId in settings.GetSubKeyNames())
        {
            report.Findings.Add(new HealthFinding
            {
                Category = HealthCategory.DisturbingNotifications,
                Target = appId,
                DisplayName = appId,
                Detail = "Can show toast notifications.",
            });
        }
    }

    private async Task AddMalwareFindingsAsync(HealthReport report, HealthScanOptions options, CancellationToken cancellationToken)
    {
        foreach (string executable in StartupExecutables())
        {
            bool localSuspicion = executable.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);
            string sha256 = HashFile(executable);

            VirusTotalVerdict? verdict = null;
            if (options.UseOnlineLookup && _virusTotal.IsEnabled && sha256.Length > 0)
            {
                verdict = await _virusTotal.CheckSha256Async(sha256, cancellationToken);
            }

            if (localSuspicion || verdict?.IsSuspicious == true)
            {
                report.Findings.Add(new HealthFinding
                {
                    Category = HealthCategory.MaliciousSoftware,
                    Target = executable,
                    DisplayName = Path.GetFileName(executable),
                    Sha256 = sha256,
                    Detail = verdict?.IsSuspicious == true
                        ? $"{verdict.MaliciousDetections}/{verdict.TotalEngines} engines flag this hash."
                        : "Launches from a temporary folder.",
                });
            }
        }
    }

    private static IEnumerable<string> StartupExecutables()
    {
        foreach ((RegistryHive hive, string view) in new[]
                 {
                     (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run"),
                     (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"),
                 })
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(view, writable: false);
            if (key is null)
            {
                continue;
            }

            foreach (string value in key.GetValueNames())
            {
                if (key.GetValue(value) is string command)
                {
                    string executable = command.Trim('"');
                    if (File.Exists(executable))
                    {
                        yield return executable;
                    }
                }
            }
        }
    }

    internal static string HashFile(string path)
    {
        try
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        }
        catch (IOException)
        {
            return string.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }
}
