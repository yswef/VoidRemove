namespace VoidRemove.Core.Programs;

/// <summary>
/// A single entry read from the Windows "Add/Remove Programs" registry, normalized so the
/// rest of the engine never touches the registry directly.
/// </summary>
public sealed record ProgramRecord
{
    /// <summary>The uninstall sub-key name; a stable id for the entry.</summary>
    public required string KeyName { get; init; }

    public required string DisplayName { get; init; }

    public string Publisher { get; init; } = string.Empty;

    public string DisplayVersion { get; init; } = string.Empty;

    public string UninstallString { get; init; } = string.Empty;

    public string QuietUninstallString { get; init; } = string.Empty;

    public string InstallLocation { get; init; } = string.Empty;

    public string DisplayIcon { get; init; } = string.Empty;

    /// <summary>"msi" for MSI based entries, otherwise empty.</summary>
    public string UninstallKind { get; init; } = string.Empty;

    /// <summary>The MSI product code (GUID) when this entry is Windows-Installer based.</summary>
    public string ProductCode { get; init; } = string.Empty;

    /// <summary>Estimated size in KB as reported by the installer.</summary>
    public long EstimatedSizeKb { get; init; }

    public DateTime? InstallDate { get; init; }

    public bool IsSystemComponent { get; init; }

    public bool IsWindowsInstaller { get; init; }

    /// <summary>Security Update / Hotfix / Update Rollup entries are handled by the updates toolbox.</summary>
    public string ReleaseType { get; init; } = string.Empty;

    /// <summary>Set on child/patch keys that hang off a parent entry.</summary>
    public string ParentKeyName { get; init; } = string.Empty;

    public ProgramSource Source { get; init; }

    public ProgramArchitecture Architecture { get; init; } = ProgramArchitecture.Unknown;

    /// <summary>True for per-user installs (HKCU).</summary>
    public bool IsUserScope => Source is ProgramSource.Hkcu or ProgramSource.Hkcu32Bit;

    /// <summary>A short label such as "x64 · All users" used by the UI tags.</summary>
    public string ScopeLabel => IsUserScope ? "User" : "All users";
}
