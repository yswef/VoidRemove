namespace VoidRemove.Shared.Contracts;

/// <summary>
/// Limits shared by the settings editor and the settings validator, so the UI can never
/// offer a value that the core would reject.
/// </summary>
public static class SettingsLimits
{
    public const int MinQuarantineRetentionDays = 1;
    public const int MaxQuarantineRetentionDays = 365;
    public const int DefaultQuarantineRetentionDays = 30;

    public const int MinMaxConcurrentScans = 1;
    public const int MaxMaxConcurrentScans = 16;
    public const int DefaultMaxConcurrentScans = 4;

    /// <summary>Folder names that may never be used as the quarantine target.</summary>
    public static IReadOnlyList<string> ForbiddenQuarantineNames { get; } = new[]
    {
        @"c:\windows",
        @"c:\windows\system32",
        @"c:\program files",
        @"c:\program files (x86)",
        @"c:\programdata",
        @"c:\users",
    };
}
