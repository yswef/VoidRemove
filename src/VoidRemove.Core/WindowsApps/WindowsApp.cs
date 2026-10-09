namespace VoidRemove.Core.WindowsApps;

/// <summary>How safe it is to remove a packaged app.</summary>
public enum WindowsAppTag
{
    Safe,
    Dependent,
    System,
}

/// <summary>One packaged (Store/UWP/MSIX) application.</summary>
public sealed record WindowsApp
{
    public required string Name { get; init; }

    public required string PackageFamilyName { get; init; }

    public string Version { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public bool IsFramework { get; init; }

    public bool IsDependency { get; init; }

    public bool IsProvisioned { get; init; }

    public WindowsAppTag Tag { get; init; } = WindowsAppTag.Safe;

    /// <summary>Fixed rule: frameworks and system-signed packages are protected.</summary>
    public bool IsProtected => Tag is WindowsAppTag.System;

    public static WindowsAppTag ClassifyTag(bool isFramework, bool isDependency, string signatureKind)
    {
        if (isFramework || signatureKind.Equals("System", StringComparison.OrdinalIgnoreCase))
        {
            return WindowsAppTag.System;
        }

        return isDependency ? WindowsAppTag.Dependent : WindowsAppTag.Safe;
    }
}
