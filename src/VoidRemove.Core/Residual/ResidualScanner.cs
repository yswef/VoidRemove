using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace VoidRemove.Core.Residual;

/// <summary>
/// Walks the well-known residual roots (program folders, app-data, start menu, shortcuts and
/// the registry) and returns their top-level entries as candidates. Roots are scanned in
/// parallel, capped by the configured concurrency. Off-Windows it yields nothing.
/// </summary>
public sealed class ResidualScanner : IResidualScanner
{
    private readonly ILogger<ResidualScanner>? _logger;

    public ResidualScanner(ILogger<ResidualScanner>? logger = null) => _logger = logger;

    public bool IsSupported => System.OperatingSystem.IsWindows();

    public async Task<IReadOnlyList<ResidualCandidate>> ScanAsync(int maxConcurrency, CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return Array.Empty<ResidualCandidate>();
        }

        int concurrency = Math.Clamp(maxConcurrency, 1, 16);
        using SemaphoreSlim throttle = new(concurrency);

        List<Func<CancellationToken, List<ResidualCandidate>>> jobs = BuildJobs();
        var tasks = jobs.Select(async job =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                return SafeRun(job, cancellationToken);
            }
            finally
            {
                throttle.Release();
            }
        }).ToArray();

        List<ResidualCandidate>[] results = await Task.WhenAll(tasks);

        return results
            .SelectMany(static r => r)
            .GroupBy(static c => c.Kind + "|" + c.Path, StringComparer.OrdinalIgnoreCase)
            .Select(static g => g.First())
            .ToList();
    }

    private List<ResidualCandidate> SafeRun(Func<CancellationToken, List<ResidualCandidate>> job, CancellationToken cancellationToken)
    {
        try
        {
            return job(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _logger?.LogDebug(exception, "A residual root could not be scanned.");
            return new List<ResidualCandidate>();
        }
    }

    private List<Func<CancellationToken, List<ResidualCandidate>>> BuildJobs()
    {
        List<Func<CancellationToken, List<ResidualCandidate>>> jobs = new();

        foreach (string root in FileSystemRoots())
        {
            jobs.Add(_ => ScanDirectories(root));
        }

        foreach (string root in ShortcutRoots())
        {
            jobs.Add(_ => ScanShortcuts(root));
        }

        jobs.Add(_ => ScanRegistrySubKeys(RegistryHive.LocalMachine, @"Software"));
        jobs.Add(_ => ScanRegistrySubKeys(RegistryHive.CurrentUser, @"Software"));
        jobs.Add(_ => ScanRegistrySubKeys(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run"));
        jobs.Add(_ => ScanRegistrySubKeys(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"));
        jobs.Add(_ => ScanRegistrySubKeys(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\App Paths"));
        jobs.Add(_ => ScanRegistryValues(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"));

        return jobs;
    }

    // ------------------------------------------------------- roots

    private static IEnumerable<string> FileSystemRoots()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Path.Combine(userProfile, "AppData", "LocalLow");
        yield return Path.GetTempPath();
    }

    private static IEnumerable<string> ShortcutRoots()
    {
        string commonStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
        string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);

        yield return Path.Combine(commonStartMenu, "Programs");
        yield return Path.Combine(startMenu, "Programs");
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    // ------------------------------------------------------- scanners

    private static List<ResidualCandidate> ScanDirectories(string root)
    {
        List<ResidualCandidate> candidates = new();
        if (!Directory.Exists(root))
        {
            return candidates;
        }

        foreach (string directory in Directory.EnumerateDirectories(root))
        {
            candidates.Add(new ResidualCandidate { Kind = ResidualKind.Directory, Path = directory });
        }

        foreach (string file in Directory.EnumerateFiles(root))
        {
            candidates.Add(new ResidualCandidate
            {
                Kind = ResidualKind.File,
                Path = file,
                SizeBytes = QuarantineSize(file),
            });
        }

        return candidates;
    }

    private static List<ResidualCandidate> ScanShortcuts(string root)
    {
        List<ResidualCandidate> candidates = new();
        if (!Directory.Exists(root))
        {
            return candidates;
        }

        foreach (string shortcut in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
        {
            candidates.Add(new ResidualCandidate { Kind = ResidualKind.Shortcut, Path = shortcut });
        }

        return candidates;
    }

    private static List<ResidualCandidate> ScanRegistrySubKeys(RegistryHive hive, string keyPath)
    {
        List<ResidualCandidate> candidates = new();
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(keyPath, writable: false);
        if (key is null)
        {
            return candidates;
        }

        string hiveName = hive == RegistryHive.LocalMachine ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER";
        foreach (string subKey in key.GetSubKeyNames())
        {
            candidates.Add(new ResidualCandidate
            {
                Kind = ResidualKind.RegistryKey,
                Path = hiveName + "\\" + keyPath + "\\" + subKey,
            });
        }

        return candidates;
    }

    private static List<ResidualCandidate> ScanRegistryValues(RegistryHive hive, string keyPath)
    {
        List<ResidualCandidate> candidates = new();
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(keyPath, writable: false);
        if (key is null)
        {
            return candidates;
        }

        string hiveName = hive == RegistryHive.LocalMachine ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER";
        foreach (string valueName in key.GetValueNames())
        {
            if (valueName.Length > 0)
            {
                candidates.Add(new ResidualCandidate
                {
                    Kind = ResidualKind.RegistryValue,
                    Path = hiveName + "\\" + keyPath + "\\" + valueName,
                });
            }
        }

        return candidates;
    }

    private static long QuarantineSize(string file)
    {
        try
        {
            return new FileInfo(file).Length;
        }
        catch (IOException)
        {
            return 0;
        }
    }
}
