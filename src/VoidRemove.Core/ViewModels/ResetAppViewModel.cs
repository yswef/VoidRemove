using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Core.AppData;
using VoidRemove.Core.Monitor;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Core.ViewModels;

/// <summary>Reset App: three-layer discovery, selective reset, backup + restore, dry run.</summary>
public sealed partial class ResetAppViewModel : ObservableObject
{
    private readonly AppDataResolver _resolver;
    private readonly ProfileCatalog _profiles;
    private readonly ResetEngine _engine;
    private readonly IInstallMonitorStore _monitorStore;
    private readonly IConfirmationDialog _confirmation;
    private readonly IStatusMessenger _messenger;
    private readonly ILocalizationService _localization;

    private string? _lastBackup;

    [ObservableProperty]
    private string _programName = string.Empty;

    [ObservableProperty]
    private bool _dryRun;

    public ResetAppViewModel(
        AppDataResolver resolver,
        ProfileCatalog profiles,
        ResetEngine engine,
        IInstallMonitorStore monitorStore,
        IConfirmationDialog confirmation,
        IStatusMessenger messenger,
        ILocalizationService localization)
    {
        _resolver = resolver;
        _profiles = profiles;
        _engine = engine;
        _monitorStore = monitorStore;
        _confirmation = confirmation;
        _messenger = messenger;
        _localization = localization;

        Locations = new ObservableCollection<ResolvedLocation>();
    }

    public ObservableCollection<ResolvedLocation> Locations { get; }

    public string Title => _localization[LocalizationKeys.NavReset];

    [RelayCommand]
    private async Task DiscoverAsync()
    {
        Locations.Clear();

        IReadOnlyList<AppDataProfile> profiles = _profiles.LoadFromDirectory(ProgramsViewModel.DataFilePath("profiles"));
        IReadOnlyList<InstallLog> logs = await _monitorStore.ListAsync();
        List<string> monitorPaths = logs.SelectMany(static log => log.AddedFiles).ToList();

        foreach (ResolvedLocation location in _resolver.Resolve(ProgramName, profiles, monitorPaths))
        {
            Locations.Add(location);
        }
    }

    [RelayCommand]
    private async Task ExecuteResetAsync()
    {
        if (Locations.Count == 0)
        {
            return;
        }

        bool confirmed = await _confirmation.ConfirmAsync(
            _localization[LocalizationKeys.CommonReset],
            _localization.Format(LocalizationKeys.ResetConfirm, ProgramName),
            _localization[LocalizationKeys.CommonConfirm],
            _localization[LocalizationKeys.CommonCancel]);

        if (!confirmed)
        {
            return;
        }

        ResetPlan plan = _engine.BuildPlan(Locations.ToList(), DryRun);
        ResetResult result = await _engine.ExecuteAsync(plan, ProgramName);

        if (result.BackupZipPath is not null)
        {
            _lastBackup = result.BackupZipPath;
        }

        _messenger.ShowSuccess(_localization.Format(LocalizationKeys.ResetDone, result.RemovedCount));
    }

    [RelayCommand]
    private async Task RestoreLastAsync()
    {
        if (_lastBackup is null)
        {
            return;
        }

        int restored = await _engine.RestoreAsync(_lastBackup);
        _messenger.ShowSuccess(_localization.Format(LocalizationKeys.ResetDone, restored));
    }
}
