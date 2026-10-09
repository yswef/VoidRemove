using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VoidRemove.Core.Storage;

namespace VoidRemove.Core.Backup;

/// <summary>
/// Captures a live registry key as a snapshot and persists it as a restorable ".reg" file
/// under <c>%ProgramData%\VoidRemove\Backups\registry</c> before the key is removed.
/// </summary>
public sealed class RegistryBackupService
{
    private readonly AppPaths _paths;
    private readonly ILogger<RegistryBackupService>? _logger;

    public RegistryBackupService(AppPaths paths, ILogger<RegistryBackupService>? logger = null)
    {
        _paths = paths;
        _logger = logger;
    }

    public bool IsSupported => System.OperatingSystem.IsWindows();

    /// <summary>Backs up a key; returns the written file, or null when unsupported/missing.</summary>
    public async Task<string?> BackupKeyAsync(string hiveName, string keyPath, string label, CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return null;
        }

        RegKeySnapshot? snapshot = Capture(hiveName, keyPath);
        if (snapshot is null)
        {
            _logger?.LogDebug("Nothing to back up at {Hive}\\{Key}.", hiveName, keyPath);
            return null;
        }

        string directory = Path.Combine(_paths.Backups, "registry");
        Directory.CreateDirectory(directory);

        string file = Path.Combine(
            directory,
            $"{SanitizeFile(label)}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.reg");

        await File.WriteAllTextAsync(file, RegExportWriter.Write(snapshot), cancellationToken);
        _logger?.LogInformation("Registry key backed up to {File}.", file);
        return file;
    }

    // ------------------------------------------------------- live capture

    internal static RegKeySnapshot? Capture(string hiveName, string keyPath)
    {
        if (!TryResolveHive(hiveName, out RegistryHive hive))
        {
            return null;
        }

        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(keyPath, writable: false);
        return key is null ? null : CaptureKey(key, MapHiveName(hive) + "\\" + keyPath);
    }

    private static RegKeySnapshot CaptureKey(RegistryKey key, string fullPath)
    {
        List<RegValueSnapshot> values = new();

        foreach (string valueName in key.GetValueNames())
        {
            values.Add(new RegValueSnapshot(
                valueName,
                MapKind(key.GetValueKind(valueName)),
                key.GetValue(valueName)));
        }

        List<RegKeySnapshot> subKeys = new();
        foreach (string subKeyName in key.GetSubKeyNames())
        {
            using RegistryKey? subKey = key.OpenSubKey(subKeyName, writable: false);
            if (subKey is not null)
            {
                subKeys.Add(CaptureKey(subKey, fullPath + "\\" + subKeyName));
            }
        }

        return new RegKeySnapshot(fullPath, values, subKeys);
    }

    internal static bool TryResolveHive(string hiveName, out RegistryHive hive)
    {
        hive = hiveName.ToUpperInvariant() switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKU" or "HKEY_USERS" => RegistryHive.Users,
            _ => (RegistryHive)(-1),
        };

        return (int)hive is not -1;
    }

    private static string MapHiveName(RegistryHive hive) => hive switch
    {
        RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
        RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
        RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
        RegistryHive.Users => "HKEY_USERS",
        _ => hive.ToString(),
    };

    private static RegValueKind MapKind(RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.String => RegValueKind.Sz,
        RegistryValueKind.ExpandString => RegValueKind.ExpandSz,
        RegistryValueKind.DWord => RegValueKind.Dword,
        RegistryValueKind.QWord => RegValueKind.Qword,
        RegistryValueKind.MultiString => RegValueKind.MultiSz,
        _ => RegValueKind.Binary,
    };

    private static string SanitizeFile(string label)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(label.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
