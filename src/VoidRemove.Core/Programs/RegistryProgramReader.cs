using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VoidRemove.Core.Uninstall;

namespace VoidRemove.Core.Programs;

/// <summary>
/// Reads the four uninstall hives (HKLM/HKCU × 64/32-bit) and merges them into a single,
/// de-duplicated program list. All value parsing is in static helpers so it can be unit
/// tested without touching the registry.
/// </summary>
public sealed class RegistryProgramReader : IProgramReader
{
    private const string UninstallSubKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    private readonly ILogger<RegistryProgramReader>? _logger;

    public RegistryProgramReader(ILogger<RegistryProgramReader>? logger = null) => _logger = logger;

    /// <inheritdoc />
    public bool IsSupported => System.OperatingSystem.IsWindows();

    /// <inheritdoc />
    public Task<IReadOnlyList<ProgramRecord>> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return Task.FromResult<IReadOnlyList<ProgramRecord>>(Array.Empty<ProgramRecord>());
        }

        List<ProgramRecord> records = new();

        foreach ((RegistryHive hive, RegistryView view, ProgramSource source) in HiveViews())
        {
            ReadHive(hive, view, source, records);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return Task.FromResult<IReadOnlyList<ProgramRecord>>(MergeDuplicates(records));
    }

    private static IEnumerable<(RegistryHive, RegistryView, ProgramSource)> HiveViews()
    {
        yield return (RegistryHive.LocalMachine, RegistryView.Registry64, ProgramSource.Hklm);
        yield return (RegistryHive.LocalMachine, RegistryView.Registry32, ProgramSource.Hklm32Bit);
        yield return (RegistryHive.CurrentUser, RegistryView.Registry64, ProgramSource.Hkcu);
        yield return (RegistryHive.CurrentUser, RegistryView.Registry32, ProgramSource.Hkcu32Bit);
    }

    private void ReadHive(RegistryHive hive, RegistryView view, ProgramSource source, List<ProgramRecord> sink)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
            using RegistryKey? uninstall = baseKey.OpenSubKey(UninstallSubKey, writable: false);
            if (uninstall is null)
            {
                return;
            }

            foreach (string subKeyName in uninstall.GetSubKeyNames())
            {
                using RegistryKey? entry = uninstall.OpenSubKey(subKeyName, writable: false);
                if (entry is null)
                {
                    continue;
                }

                ProgramRecord? record = TryBuild(entry, subKeyName, source);
                if (record is not null)
                {
                    sink.Add(record);
                }
            }
        }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            _logger?.LogWarning(exception, "Could not read uninstall hive {Hive}/{View}.", hive, view);
        }
    }

    private static ProgramRecord? TryBuild(RegistryKey entry, string keyName, ProgramSource source)
    {
        string displayName = GetString(entry, "DisplayName");
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        // Hidden or subordinate entries are not standalone programs.
        if (GetDword(entry, "SystemComponent") == 1 || !string.IsNullOrEmpty(GetString(entry, "ParentKeyName")))
        {
            return null;
        }

        string releaseType = GetString(entry, "ReleaseType");
        bool windowsInstaller = GetDword(entry, "WindowsInstaller") == 1;
        string uninstallString = GetString(entry, "UninstallString");

        string productCode = windowsInstaller
            ? UninstallStringParser.ExtractProductCode(uninstallString) ?? keyName
            : string.Empty;

        return new ProgramRecord
        {
            KeyName = keyName,
            DisplayName = displayName.Trim(),
            Publisher = GetString(entry, "Publisher").Trim(),
            DisplayVersion = GetString(entry, "DisplayVersion").Trim(),
            UninstallString = uninstallString,
            QuietUninstallString = GetString(entry, "QuietUninstallString"),
            InstallLocation = GetString(entry, "InstallLocation").Trim(),
            DisplayIcon = GetString(entry, "DisplayIcon"),
            UninstallKind = windowsInstaller ? "msi" : string.Empty,
            ProductCode = productCode,
            EstimatedSizeKb = GetDword(entry, "EstimatedSize"),
            InstallDate = ParseInstallDate(GetString(entry, "InstallDate")),
            IsSystemComponent = false,
            IsWindowsInstaller = windowsInstaller,
            ReleaseType = releaseType,
            ParentKeyName = string.Empty,
            Source = source,
            Architecture = DetectArchitecture(source, GetString(entry, "InstallLocation")),
        };
    }

    // ------------------------------------------------------- value helpers

    internal static string GetString(RegistryKey key, string name) =>
        key.GetValue(name) is string value ? value : string.Empty;

    internal static long GetDword(RegistryKey key, string name) =>
        key.GetValue(name) switch
        {
            int intValue => intValue,
            long longValue => longValue,
            _ => 0,
        };

    /// <summary>Parses the "yyyymmdd" install date format used by the uninstall registry.</summary>
    internal static DateTime? ParseInstallDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string digits = new(raw.Where(char.IsDigit).ToArray());
        return digits.Length == 8 &&
               int.TryParse(digits.Substring(0, 4), out int year) &&
               int.TryParse(digits.Substring(4, 2), out int month) &&
               int.TryParse(digits.Substring(6, 2), out int day) &&
               year >= 1980 && month is >= 1 and <= 12 && day is >= 1 and <= 31
            ? new DateTime(year, month, day)
            : null;
    }

    internal static ProgramArchitecture DetectArchitecture(ProgramSource source, string installLocation)
    {
        if (source is ProgramSource.Hklm32Bit or ProgramSource.Hkcu32Bit ||
            installLocation.Contains("Program Files (x86)", StringComparison.OrdinalIgnoreCase))
        {
            return ProgramArchitecture.X86;
        }

        return source is ProgramSource.Hklm or ProgramSource.Hkcu ? ProgramArchitecture.X64 : ProgramArchitecture.Unknown;
    }

    // ------------------------------------------------------- merge

    private static List<ProgramRecord> MergeDuplicates(List<ProgramRecord> records)
    {
        Dictionary<string, ProgramRecord> best = new(StringComparer.OrdinalIgnoreCase);

        foreach (ProgramRecord record in records.OrderByDescending(static r => SourcePriority(r.Source)))
        {
            string identity = record.DisplayName + "|" + record.Publisher;
            best.TryAdd(identity, record);
        }

        return best.Values
            .OrderBy(static r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int SourcePriority(ProgramSource source) => source switch
    {
        ProgramSource.Hklm => 4,
        ProgramSource.Hklm32Bit => 3,
        ProgramSource.Hkcu => 2,
        ProgramSource.Hkcu32Bit => 1,
        _ => 0,
    };
}
