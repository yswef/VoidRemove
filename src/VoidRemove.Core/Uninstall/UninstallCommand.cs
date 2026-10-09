namespace VoidRemove.Core.Uninstall;

/// <summary>How an uninstaller is invoked.</summary>
public enum UninstallKind
{
    /// <summary>Could not be classified; the engine will only offer a manual route.</summary>
    Unknown,

    /// <summary>Windows Installer; run through msiexec with the product code.</summary>
    MsiExec,

    /// <summary>A native uninstaller executable with arguments.</summary>
    Custom,

    /// <summary>A packaged (MSIX/AppX) app removed through the package manager.</summary>
    Msix,
}

/// <summary>The parsed, runnable form of an UninstallString.</summary>
public sealed record UninstallCommand
{
    public UninstallKind Kind { get; init; } = UninstallKind.Unknown;

    /// <summary>Executable to launch (msiexec.exe, the uninstaller exe, or empty for MSIX).</summary>
    public string Executable { get; init; } = string.Empty;

    /// <summary>Argument tail passed to <see cref="Executable"/>.</summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>Normalized MSI product code, "{GUID}".</summary>
    public string ProductCode { get; init; } = string.Empty;

    /// <summary>Package family name for MSIX entries.</summary>
    public string PackageFamilyName { get; init; } = string.Empty;

    /// <summary>The original string, for reporting.</summary>
    public string Raw { get; init; } = string.Empty;

    public bool IsRunnable => Kind is not UninstallKind.Unknown;
}
