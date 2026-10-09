using System.Text.Json;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Models;
using VoidRemove.Shared.Theming;

namespace VoidRemove.Core.Settings;

/// <summary>
/// Everything VoidRemove persists, plus the machine state it remembers between runs.
/// </summary>
/// <remarks>
/// This is a superset of <see cref="SettingsSnapshot"/>: the snapshot is what the user
/// edits, this class also carries the schema version and the timestamps other features
/// record. Secrets are plain text in memory and are encrypted by
/// <see cref="SettingsService"/> before they reach the disk.
/// </remarks>
public sealed class AppSettings
{
    /// <summary>Bumped when the shape of the file changes so older files can be migrated.</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    // ---- Appearance ----
    public string Language { get; set; } = SettingsSnapshot.LanguageOptionDefault;
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public string AccentColor { get; set; } = SettingsSnapshot.DefaultAccent;
    public LayoutMode Layout { get; set; } = LayoutMode.List;

    // ---- Uninstall behavior ----
    public UninstallBehavior DefaultUninstallBehavior { get; set; } = UninstallBehavior.Interactive;
    public bool CreateRestorePointByDefault { get; set; } = true;
    public bool RemoveResidualsByDefault { get; set; } = true;
    public SafetyLevel ResidualSafetyCeiling { get; set; } = SafetyLevel.Moderate;
    public bool ConfirmBeforeUninstall { get; set; } = true;
    public bool DryRunByDefault { get; set; }

    // ---- Monitoring / shell integration ----
    public bool AutoMonitorEnabled { get; set; }
    public bool ExplorerContextMenuEnabled { get; set; }

    // ---- Opt-in online features ----
    public bool CheckForUpdatesEnabled { get; set; }
    public bool VirusTotalEnabled { get; set; }
    public string VirusTotalApiKey { get; set; } = string.Empty;

    // ---- Storage ----
    public string QuarantinePath { get; set; } = string.Empty;
    public int QuarantineRetentionDays { get; set; } = SettingsLimits.DefaultQuarantineRetentionDays;
    public int MaxConcurrentScans { get; set; } = SettingsLimits.DefaultMaxConcurrentScans;

    // ---- Machine state ----
    public DateTime? LastHealthScanUtc { get; set; }
    public DateTime? LastUpdateCheckUtc { get; set; }

    /// <summary>Ids of Action Center recommendations the user dismissed.</summary>
    public List<string> DismissedRecommendations { get; set; } = new();

    /// <summary>Factory defaults.</summary>
    public static AppSettings CreateDefault() => new();

    /// <summary>Deep copy via the serializer, so the persisted shape and the copy never diverge.</summary>
    public AppSettings Clone()
    {
        string json = JsonSerializer.Serialize(this, SettingsJson.Options);
        return JsonSerializer.Deserialize<AppSettings>(json, SettingsJson.Options) ?? CreateDefault();
    }

    /// <summary>Projects the user editable part of the settings.</summary>
    public SettingsSnapshot ToSnapshot() => new()
    {
        Language = Language,
        Theme = Theme,
        AccentColor = AccentColor,
        Layout = Layout,
        DefaultUninstallBehavior = DefaultUninstallBehavior,
        CreateRestorePointByDefault = CreateRestorePointByDefault,
        RemoveResidualsByDefault = RemoveResidualsByDefault,
        ResidualSafetyCeiling = ResidualSafetyCeiling,
        ConfirmBeforeUninstall = ConfirmBeforeUninstall,
        DryRunByDefault = DryRunByDefault,
        AutoMonitorEnabled = AutoMonitorEnabled,
        ExplorerContextMenuEnabled = ExplorerContextMenuEnabled,
        CheckForUpdatesEnabled = CheckForUpdatesEnabled,
        VirusTotalEnabled = VirusTotalEnabled,
        VirusTotalApiKey = VirusTotalApiKey,
        QuarantinePath = QuarantinePath,
        QuarantineRetentionDays = QuarantineRetentionDays,
        MaxConcurrentScans = MaxConcurrentScans,
    };

    /// <summary>Copies every user editable value from a snapshot, keeping machine state intact.</summary>
    public void ApplySnapshot(SettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Language = snapshot.Language;
        Theme = snapshot.Theme;
        AccentColor = snapshot.AccentColor;
        Layout = snapshot.Layout;
        DefaultUninstallBehavior = snapshot.DefaultUninstallBehavior;
        CreateRestorePointByDefault = snapshot.CreateRestorePointByDefault;
        RemoveResidualsByDefault = snapshot.RemoveResidualsByDefault;
        ResidualSafetyCeiling = snapshot.ResidualSafetyCeiling;
        ConfirmBeforeUninstall = snapshot.ConfirmBeforeUninstall;
        DryRunByDefault = snapshot.DryRunByDefault;
        AutoMonitorEnabled = snapshot.AutoMonitorEnabled;
        ExplorerContextMenuEnabled = snapshot.ExplorerContextMenuEnabled;
        CheckForUpdatesEnabled = snapshot.CheckForUpdatesEnabled;
        VirusTotalEnabled = snapshot.VirusTotalEnabled;
        VirusTotalApiKey = snapshot.VirusTotalApiKey;
        QuarantinePath = snapshot.QuarantinePath;
        QuarantineRetentionDays = snapshot.QuarantineRetentionDays;
        MaxConcurrentScans = snapshot.MaxConcurrentScans;
    }
}
