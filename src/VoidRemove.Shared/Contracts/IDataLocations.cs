namespace VoidRemove.Shared.Contracts;

/// <summary>
/// The folders VoidRemove owns on this machine, all under <c>%ProgramData%\VoidRemove</c>.
/// </summary>
public interface IDataLocations
{
    /// <summary>Root folder, e.g. <c>C:\ProgramData\VoidRemove</c>.</summary>
    string Root { get; }

    /// <summary>Serilog rolling log files.</summary>
    string Logs { get; }

    /// <summary>Files moved out of the way before deletion, restorable until retention expires.</summary>
    string Quarantine { get; }

    /// <summary>Exported .reg files and ZIP snapshots taken before a destructive action.</summary>
    string Backups { get; }

    /// <summary>SQLite caches: program list, scan results, install monitor log.</summary>
    string Cache { get; }

    /// <summary>The settings file itself.</summary>
    string SettingsFile { get; }
}
