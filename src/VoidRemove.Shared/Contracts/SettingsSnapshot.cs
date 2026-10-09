using VoidRemove.Shared.Models;
using VoidRemove.Shared.Theming;

namespace VoidRemove.Shared.Contracts;

/// <summary>
/// The subset of the persisted settings that the user can edit.
/// </summary>
/// <remarks>
/// A record with <c>init</c> properties so a view model can hold a draft, compare it with
/// the saved copy and submit it as one immutable object. Secrets travel in memory as plain
/// text and are encrypted by <c>VoidRemove.Core</c> before they touch the disk.
/// </remarks>
public sealed record SettingsSnapshot
{
    // ---- Appearance ----
    public string Language { get; init; } = LanguageOptionDefault;
    public AppTheme Theme { get; init; } = AppTheme.Dark;
    public string AccentColor { get; init; } = DefaultAccent;
    public LayoutMode Layout { get; init; } = LayoutMode.List;

    // ---- Uninstall behavior ----
    public UninstallBehavior DefaultUninstallBehavior { get; init; } = UninstallBehavior.Interactive;
    public bool CreateRestorePointByDefault { get; init; } = true;
    public bool RemoveResidualsByDefault { get; init; } = true;
    public SafetyLevel ResidualSafetyCeiling { get; init; } = SafetyLevel.Moderate;
    public bool ConfirmBeforeUninstall { get; init; } = true;
    public bool DryRunByDefault { get; init; }

    // ---- Monitoring / shell integration ----
    public bool AutoMonitorEnabled { get; init; }
    public bool ExplorerContextMenuEnabled { get; init; }

    // ---- Opt-in online features (both off by default) ----
    public bool CheckForUpdatesEnabled { get; init; }
    public bool VirusTotalEnabled { get; init; }
    public string VirusTotalApiKey { get; init; } = string.Empty;

    // ---- Storage ----
    public string QuarantinePath { get; init; } = string.Empty;
    public int QuarantineRetentionDays { get; init; } = SettingsLimits.DefaultQuarantineRetentionDays;
    public int MaxConcurrentScans { get; init; } = SettingsLimits.DefaultMaxConcurrentScans;

    /// <summary>Accent color shipped with the identity.</summary>
    public const string DefaultAccent = "#2EE6A8";

    /// <summary>Language used until the user picks one.</summary>
    public const string LanguageOptionDefault = "en";

    /// <summary>Defaults in one place, used by "Restore defaults" and by first run.</summary>
    public static SettingsSnapshot CreateDefault() => new();
}
