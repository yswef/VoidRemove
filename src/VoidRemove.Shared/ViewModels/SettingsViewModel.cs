using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Models;
using VoidRemove.Shared.Theming;

namespace VoidRemove.Shared.ViewModels;

/// <summary>
/// The settings page. Holds an editable draft, validates it through the core validator and
/// writes it back as one immutable snapshot.
/// </summary>
/// <remarks>
/// Theme and language changes preview live; everything else is applied on save. The
/// validator lives in <c>VoidRemove.Core</c> so the WPF and WinUI pages can never disagree
/// about what is a legal value.
/// </remarks>
public sealed partial class SettingsViewModel : ObservableObject
{
    private static readonly HashSet<string> EditableProperties = new(StringComparer.Ordinal)
    {
        nameof(Language),
        nameof(Theme),
        nameof(AccentColor),
        nameof(Layout),
        nameof(DefaultUninstallBehavior),
        nameof(CreateRestorePointByDefault),
        nameof(RemoveResidualsByDefault),
        nameof(ResidualSafetyCeiling),
        nameof(ConfirmBeforeUninstall),
        nameof(DryRunByDefault),
        nameof(AutoMonitorEnabled),
        nameof(ExplorerContextMenuEnabled),
        nameof(CheckForUpdatesEnabled),
        nameof(VirusTotalEnabled),
        nameof(VirusTotalApiKey),
        nameof(QuarantinePath),
        nameof(QuarantineRetentionDays),
        nameof(MaxConcurrentScans),
    };

    private readonly ILocalizationService _localization;
    private readonly ISettingsController _settings;
    private readonly IConfirmationDialog _confirmation;
    private readonly ThemeManager _themes;
    private readonly IStatusMessenger _messenger;

    private SettingsSnapshot _saved = SettingsSnapshot.CreateDefault();
    private bool _loading;

    public SettingsViewModel(
        ILocalizationService localization,
        ISettingsController settings,
        IConfirmationDialog confirmation,
        ThemeManager themes,
        IStatusMessenger messenger)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _confirmation = confirmation ?? throw new ArgumentNullException(nameof(confirmation));
        _themes = themes ?? throw new ArgumentNullException(nameof(themes));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

        List<LocalizedChoice<AppTheme>> themeOptions = new();
        foreach (ThemeDefinition definition in ThemeCatalog.Themes)
        {
            themeOptions.Add(new LocalizedChoice<AppTheme>(definition.Id, definition.DisplayNameKey, localization));
        }

        ThemeOptions = themeOptions;
        LayoutOptions = LocalizedChoiceCatalog.Layouts(localization);
        BehaviorOptions = LocalizedChoiceCatalog.UninstallBehaviors(localization);
        SafetyOptions = LocalizedChoiceCatalog.SafetyLevels(localization);

        _settings.SettingsApplied += OnSettingsApplied;
        Load(_settings.Snapshot);
    }

    // ---- Draft state ----

    [ObservableProperty]
    private string _language = SettingsSnapshot.LanguageOptionDefault;

    [ObservableProperty]
    private AppTheme _theme = AppTheme.Dark;

    [ObservableProperty]
    private string _accentColor = SettingsSnapshot.DefaultAccent;

    [ObservableProperty]
    private LayoutMode _layout;

    [ObservableProperty]
    private UninstallBehavior _defaultUninstallBehavior;

    [ObservableProperty]
    private bool _createRestorePointByDefault = true;

    [ObservableProperty]
    private bool _removeResidualsByDefault = true;

    [ObservableProperty]
    private SafetyLevel _residualSafetyCeiling = SafetyLevel.Moderate;

    [ObservableProperty]
    private bool _confirmBeforeUninstall = true;

    [ObservableProperty]
    private bool _dryRunByDefault;

    [ObservableProperty]
    private bool _autoMonitorEnabled;

    [ObservableProperty]
    private bool _explorerContextMenuEnabled;

    [ObservableProperty]
    private bool _checkForUpdatesEnabled;

    [ObservableProperty]
    private bool _virusTotalEnabled;

    [ObservableProperty]
    private string _virusTotalApiKey = string.Empty;

    [ObservableProperty]
    private string _quarantinePath = string.Empty;

    [ObservableProperty]
    private int _quarantineRetentionDays = SettingsLimits.DefaultQuarantineRetentionDays;

    [ObservableProperty]
    private int _maxConcurrentScans = SettingsLimits.DefaultMaxConcurrentScans;

    [ObservableProperty]
    private bool _isBusy;

    // ---- Pickers ----

    public IReadOnlyList<LanguageOption> Languages => _localization.Languages;
    public IReadOnlyList<LocalizedChoice<AppTheme>> ThemeOptions { get; }
    public IReadOnlyList<LocalizedChoice<LayoutMode>> LayoutOptions { get; }
    public IReadOnlyList<LocalizedChoice<UninstallBehavior>> BehaviorOptions { get; }
    public IReadOnlyList<LocalizedChoice<SafetyLevel>> SafetyOptions { get; }

    /// <summary>Accent swatches offered next to the free text field.</summary>
    public IReadOnlyList<string> AccentPresets { get; } = new[]
    {
        "#2EE6A8", "#7B61FF", "#35E0D0", "#4CC2FF", "#F5B840", "#FF5470",
    };

    public int MinRetentionDays => SettingsLimits.MinQuarantineRetentionDays;
    public int MaxRetentionDays => SettingsLimits.MaxQuarantineRetentionDays;
    public int MinWorkers => SettingsLimits.MinMaxConcurrentScans;
    public int MaxWorkers => SettingsLimits.MaxMaxConcurrentScans;

    // ---- Labels ----

    public string Title => _localization[LocalizationKeys.SettingsTitle];
    public string Subtitle => _localization[LocalizationKeys.SettingsSubtitle];
    public string AppearanceGroupTitle => _localization[LocalizationKeys.SettingsGroupAppearance];
    public string UninstallGroupTitle => _localization[LocalizationKeys.SettingsGroupUninstall];
    public string MonitoringGroupTitle => _localization[LocalizationKeys.SettingsGroupMonitoring];
    public string OnlineGroupTitle => _localization[LocalizationKeys.SettingsGroupOnline];
    public string StorageGroupTitle => _localization[LocalizationKeys.SettingsGroupStorage];

    public string LanguageLabel => _localization[LocalizationKeys.SettingsLanguage];
    public string LanguageHint => _localization[LocalizationKeys.SettingsLanguageHint];
    public string ThemeLabel => _localization[LocalizationKeys.SettingsTheme];
    public string AccentLabel => _localization[LocalizationKeys.SettingsAccent];
    public string LayoutLabel => _localization[LocalizationKeys.SettingsLayout];
    public string UninstallBehaviorLabel => _localization[LocalizationKeys.SettingsUninstallBehavior];
    public string CreateRestorePointLabel => _localization[LocalizationKeys.SettingsCreateRestorePoint];
    public string RestorePointUnavailableHint => _localization[LocalizationKeys.SettingsRestorePointUnavailable];
    public string RemoveResidualsLabel => _localization[LocalizationKeys.SettingsRemoveResiduals];
    public string SafetyCeilingLabel => _localization[LocalizationKeys.SettingsSafetyCeiling];
    public string ConfirmBeforeUninstallLabel => _localization[LocalizationKeys.SettingsConfirmBeforeUninstall];
    public string DryRunDefaultLabel => _localization[LocalizationKeys.SettingsDryRunDefault];
    public string AutoMonitorLabel => _localization[LocalizationKeys.SettingsAutoMonitor];
    public string AutoMonitorHint => _localization[LocalizationKeys.SettingsAutoMonitorHint];
    public string ContextMenuLabel => _localization[LocalizationKeys.SettingsContextMenu];
    public string ContextMenuHint => _localization[LocalizationKeys.SettingsContextMenuHint];
    public string CheckForUpdatesLabel => _localization[LocalizationKeys.SettingsCheckForUpdates];
    public string CheckForUpdatesHint => _localization[LocalizationKeys.SettingsCheckForUpdatesHint];
    public string VirusTotalLabel => _localization[LocalizationKeys.SettingsVirusTotal];
    public string VirusTotalHint => _localization[LocalizationKeys.SettingsVirusTotalHint];
    public string VirusTotalKeyLabel => _localization[LocalizationKeys.SettingsVirusTotalKey];
    public string VirusTotalKeyHint => _localization[LocalizationKeys.SettingsVirusTotalKeyHint];
    public string QuarantinePathLabel => _localization[LocalizationKeys.SettingsQuarantinePath];
    public string QuarantineRetentionLabel => _localization[LocalizationKeys.SettingsQuarantineRetention];
    public string MaxConcurrentScansLabel => _localization[LocalizationKeys.SettingsMaxConcurrentScans];
    public string SaveText => _localization[LocalizationKeys.SettingsSave];
    public string RestoreDefaultsText => _localization[LocalizationKeys.SettingsRestoreDefaults];
    public string UnsavedChangesText => _localization[LocalizationKeys.SettingsUnsavedChanges];
    public string InvalidText => _localization[LocalizationKeys.SettingsInvalid];
    public string NeedsRestartText => _localization[LocalizationKeys.SettingsNeedsRestart];

    // ---- Derived state ----

    /// <summary>True when the draft differs from what is stored.</summary>
    public bool HasUnsavedChanges => BuildSnapshot() != _saved;

    /// <summary>Validation problems for the current draft, already localized.</summary>
    public IReadOnlyList<string> ValidationErrors => _settings.Validate(BuildSnapshot());

    /// <summary>Guards the save button.</summary>
    public bool CanSave => !IsBusy && HasUnsavedChanges && ValidationErrors.Count == 0;

    /// <summary>The snapshot currently stored on disk.</summary>
    public SettingsSnapshot Saved => _saved;

    /// <summary>Re-reads the draft from disk. Called when another component saved settings.</summary>
    public void Reload() => Load(_settings.Snapshot);

    /// <summary>Refreshes every label after a language switch.</summary>
    public void Refresh()
    {
        RefreshChoices(ThemeOptions);
        RefreshChoices(LayoutOptions);
        RefreshChoices(BehaviorOptions);
        RefreshChoices(SafetyOptions);
        OnPropertyChanged(string.Empty);
    }

    /// <summary>Applies one of the accent swatches.</summary>
    [RelayCommand]
    private void SetAccent(string? hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            AccentColor = hex;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        SettingsSnapshot candidate = BuildSnapshot();

        IReadOnlyList<string> problems = _settings.Validate(candidate);
        if (problems.Count > 0)
        {
            _messenger.ShowError(string.Join(" ", problems));
            return;
        }

        IsBusy = true;
        try
        {
            await _settings.SaveAsync(candidate).ConfigureAwait(true);
            _saved = candidate;
            _localization.SetLanguage(candidate.Language);
            _themes.Apply(candidate.Theme, candidate.AccentColor);
            _messenger.ShowSuccess(_localization[LocalizationKeys.CommonSaved]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreDefaultsAsync()
    {
        bool agreed = await _confirmation.ConfirmAsync(
            Title,
            _localization[LocalizationKeys.SettingsRestoreDefaultsConfirm],
            _localization[LocalizationKeys.CommonOk],
            _localization[LocalizationKeys.CommonCancel]).ConfigureAwait(true);

        if (!agreed)
        {
            return;
        }

        IsBusy = true;
        try
        {
            SettingsSnapshot defaults = await _settings.RestoreDefaultsAsync().ConfigureAwait(true);
            Load(defaults);
            _localization.SetLanguage(defaults.Language);
            _themes.Apply(defaults.Theme, defaults.AccentColor);
            _messenger.ShowSuccess(_localization[LocalizationKeys.HomeSettingsReset]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Builds the snapshot that would be saved right now.</summary>
    public SettingsSnapshot BuildSnapshot() => new()
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

    private void Load(SettingsSnapshot snapshot)
    {
        _loading = true;
        try
        {
            _saved = snapshot;
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
        finally
        {
            _loading = false;
        }

        OnPropertyChanged(string.Empty);
        NotifySaveStateChanged();
    }

    private void OnSettingsApplied(object? sender, SettingsSnapshot snapshot) => Load(snapshot);

    private void NotifySaveStateChanged()
    {
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(ValidationErrors));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private static void RefreshChoices<T>(IReadOnlyList<LocalizedChoice<T>> choices)
        where T : notnull
    {
        foreach (LocalizedChoice<T> choice in choices)
        {
            choice.Refresh();
        }
    }

    /// <summary>Live previews the theme and keeps the save button in sync.</summary>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_loading)
        {
            return;
        }

        if (e.PropertyName is null || EditableProperties.Contains(e.PropertyName))
        {
            NotifySaveStateChanged();
        }
    }

    partial void OnThemeChanged(AppTheme value) => _themes.Apply(value, AccentColor);

    partial void OnAccentColorChanged(string value)
    {
        // Live preview, but only for text that already parses: the field is free form and
        // would otherwise log a warning on every keystroke.
        if (RgbaColor.TryFromHex(value, out _))
        {
            _themes.Apply(Theme, value);
        }
    }

    partial void OnLanguageChanged(string value) => _localization.SetLanguage(value);

    partial void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
