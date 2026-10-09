using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Theming;

namespace VoidRemove.Shared.ViewModels;

/// <summary>
/// The landing page: what VoidRemove can see about this machine, where it stores its data,
/// and the state of every opt-in feature.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly ISystemInformation _system;
    private readonly IDataLocations _locations;
    private readonly ISettingsController _settings;
    private readonly IConfirmationDialog _confirmation;
    private readonly ThemeManager _themes;
    private readonly IExternalLauncher _launcher;
    private readonly IStatusMessenger _messenger;

    private SettingsSnapshot _snapshot;

    public HomeViewModel(
        ILocalizationService localization,
        ISystemInformation system,
        IDataLocations locations,
        ISettingsController settings,
        IConfirmationDialog confirmation,
        ThemeManager themes,
        IExternalLauncher launcher,
        IStatusMessenger messenger)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _system = system ?? throw new ArgumentNullException(nameof(system));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _confirmation = confirmation ?? throw new ArgumentNullException(nameof(confirmation));
        _themes = themes ?? throw new ArgumentNullException(nameof(themes));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _snapshot = settings.Snapshot;
    }

    // ---- Page header ----

    public string Title => _localization[LocalizationKeys.HomeTitle];
    public string Subtitle => _localization[LocalizationKeys.HomeSubtitle];
    public string Tagline => _localization[LocalizationKeys.AppTagline];
    public string License => _localization[LocalizationKeys.AppLicense];
    public string OfflineNotice => _localization[LocalizationKeys.AppOffline];
    public string VersionText => _localization.Format(LocalizationKeys.AppVersion, AppInfo.Version);

    // ---- System ----

    public string SystemGroupTitle => _localization[LocalizationKeys.HomeGroupSystem];
    public string OperatingSystemLabel => _localization[LocalizationKeys.HomeOs];
    public string ArchitectureLabel => _localization[LocalizationKeys.HomeArchitecture];
    public string AdministratorLabel => _localization[LocalizationKeys.HomeAdminRights];
    public string OperatingSystem => _system.OperatingSystem;
    public string Architecture => _system.Architecture;
    public bool IsAdministrator => _system.IsAdministrator;

    public string AdministratorState =>
        _system.IsAdministrator
            ? _localization[LocalizationKeys.HomeAdminGranted]
            : _localization[LocalizationKeys.HomeAdminMissing];

    // ---- Data locations ----

    public string LocationsGroupTitle => _localization[LocalizationKeys.HomeGroupLocations];
    public string DataFolderLabel => _localization[LocalizationKeys.HomeDataFolder];
    public string LogFolderLabel => _localization[LocalizationKeys.HomeLogFolder];
    public string QuarantineFolderLabel => _localization[LocalizationKeys.HomeQuarantineFolder];
    public string BackupsFolderLabel => _localization[LocalizationKeys.HomeBackupsFolder];
    public string DataFolder => _locations.Root;
    public string LogFolder => _locations.Logs;
    public string BackupsFolder => _locations.Backups;

    public string QuarantineFolder =>
        string.IsNullOrWhiteSpace(_snapshot.QuarantinePath) ? _locations.Quarantine : _snapshot.QuarantinePath;

    // ---- Appearance ----

    public string AppearanceGroupTitle => _localization[LocalizationKeys.HomeGroupAppearance];
    public string LanguageLabel => _localization[LocalizationKeys.HomeLanguage];
    public string ThemeLabel => _localization[LocalizationKeys.HomeTheme];
    public string LayoutLabel => _localization[LocalizationKeys.HomeLayout];
    public string AccentLabel => _localization[LocalizationKeys.HomeAccent];

    public string LanguageDisplay => _localization[_localization.CurrentLanguage switch
    {
        "ar" => LocalizationKeys.LanguageAr,
        _ => LocalizationKeys.LanguageEn,
    }];

    public string ThemeDisplay => _themes.CurrentDisplayName;
    public string AccentDisplay => _snapshot.AccentColor;

    public string LayoutDisplay => _snapshot.Layout switch
    {
        LayoutMode.Cards => _localization[LocalizationKeys.LayoutCards],
        LayoutMode.Compact => _localization[LocalizationKeys.LayoutCompact],
        _ => _localization[LocalizationKeys.LayoutList],
    };

    // ---- Feature status ----

    public string StatusGroupTitle => _localization[LocalizationKeys.HomeGroupStatus];
    public string AutoMonitorLabel => _localization[LocalizationKeys.HomeAutoMonitor];
    public string ContextMenuLabel => _localization[LocalizationKeys.HomeContextMenu];
    public string UpdatesLabel => _localization[LocalizationKeys.HomeUpdates];
    public string VirusTotalLabel => _localization[LocalizationKeys.HomeVirusTotal];
    public string AutoMonitorState => OnOff(_snapshot.AutoMonitorEnabled);
    public string ContextMenuState => OnOff(_snapshot.ExplorerContextMenuEnabled);
    public string UpdatesState => OnOff(_snapshot.CheckForUpdatesEnabled);
    public string VirusTotalState => OnOff(_snapshot.VirusTotalEnabled);

    /// <summary>Raw flags, so the UI can colour a status dot without parsing a string.</summary>
    public bool IsAutoMonitorEnabled => _snapshot.AutoMonitorEnabled;
    public bool IsContextMenuEnabled => _snapshot.ExplorerContextMenuEnabled;
    public bool IsUpdatesEnabled => _snapshot.CheckForUpdatesEnabled;
    public bool IsVirusTotalEnabled => _snapshot.VirusTotalEnabled;

    // ---- Quick actions ----

    public string ActionsGroupTitle => _localization[LocalizationKeys.HomeGroupActions];
    public string OpenSettingsFileText => _localization[LocalizationKeys.HomeOpenSettingsFile];
    public string RestoreDefaultsText => _localization[LocalizationKeys.HomeResetSettings];
    public string OpenFolderText => _localization[LocalizationKeys.CommonOpenFolder];
    public string CopyText => _localization[LocalizationKeys.CommonCopy];
    public string OpenRepositoryText => _localization[LocalizationKeys.AboutRepository];
    public string OpenDocsText => _localization[LocalizationKeys.AboutTitle];
    public string ReportIssueText => _localization[LocalizationKeys.AboutReportIssue];

    /// <summary>Re-reads the snapshot and repaints every derived label.</summary>
    public void Refresh()
    {
        _snapshot = _settings.Snapshot;
        OnPropertyChanged(string.Empty);
    }

    [RelayCommand]
    private void OpenFolder(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            _launcher.OpenFolder(path);
        }
    }

    [RelayCommand]
    private void CopyPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _launcher.CopyToClipboard(path);
        _messenger.ShowInfo(_localization[LocalizationKeys.CommonCopied]);
    }

    [RelayCommand]
    private void OpenSettingsFile() => _launcher.OpenFolder(_locations.SettingsFile);

    [RelayCommand]
    private void OpenRepository() => _launcher.OpenUrl(AppInfo.RepositoryUrl);

    [RelayCommand]
    private void OpenUserManual() => _launcher.OpenUrl(AppInfo.UserManualUrl);

    [RelayCommand]
    private void ReportIssue() => _launcher.OpenUrl(AppInfo.NewIssueUrl);

    [RelayCommand]
    private async Task RestoreDefaultsAsync()
    {
        bool agreed = await _confirmation.ConfirmAsync(
            _localization[LocalizationKeys.SettingsTitle],
            _localization[LocalizationKeys.SettingsRestoreDefaultsConfirm],
            _localization[LocalizationKeys.CommonOk],
            _localization[LocalizationKeys.CommonCancel]).ConfigureAwait(true);

        if (!agreed)
        {
            return;
        }

        _snapshot = await _settings.RestoreDefaultsAsync().ConfigureAwait(true);
        _themes.Apply(_snapshot.Theme, _snapshot.AccentColor);
        Refresh();
        _messenger.ShowSuccess(_localization[LocalizationKeys.HomeSettingsReset]);
    }

    private string OnOff(bool value) =>
        _localization[value ? LocalizationKeys.CommonOn : LocalizationKeys.CommonOff];
}
