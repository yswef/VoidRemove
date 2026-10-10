using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Core.Engine;
using VoidRemove.Core.History;
using VoidRemove.Core.Programs;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Models;

namespace VoidRemove.Core.ViewModels;

/// <summary>Total Uninstall: options (restore point, safety ceiling, dry run) + preview + run.</summary>
public sealed partial class TotalUninstallViewModel : ObservableObject
{
    private readonly IProgramReader _reader;
    private readonly UninstallEngine _engine;
    private readonly IConfirmationDialog _confirmation;
    private readonly IStatusMessenger _messenger;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private ProgramItemViewModel? _selectedProgram;

    [ObservableProperty]
    private bool _createRestorePoint;

    [ObservableProperty]
    private bool _dryRun;

    [ObservableProperty]
    private SafetyLevel _safetyCeiling = SafetyLevel.Moderate;

    [ObservableProperty]
    private int _previewCount;

    [ObservableProperty]
    private bool _isLoading;

    public TotalUninstallViewModel(
        IProgramReader reader,
        UninstallEngine engine,
        IConfirmationDialog confirmation,
        IStatusMessenger messenger,
        ILocalizationService localization)
    {
        _reader = reader;
        _engine = engine;
        _confirmation = confirmation;
        _messenger = messenger;
        _localization = localization;

        Programs = new ObservableCollection<ProgramItemViewModel>();
    }

    public ObservableCollection<ProgramItemViewModel> Programs { get; }

    public string Title => _localization[LocalizationKeys.NavUninstall];

    public string RestorePointUnavailableHint => _localization[LocalizationKeys.SettingsRestorePointUnavailable];

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            Programs.Clear();
            foreach (ProgramRecord record in await _reader.ReadAsync())
            {
                Programs.Add(new ProgramItemViewModel(record, isBundleware: false));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private UninstallOptions BuildOptions() => new()
    {
        CreateRestorePoint = CreateRestorePoint,
        DryRun = DryRun,
        MaxSafetyLevel = SafetyCeiling,
    };

    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (SelectedProgram is null)
        {
            return;
        }

        IReadOnlyList<MatchedResidual> residuals = await _engine.PreviewAsync(
            SelectedProgram.Record, BuildOptions(), maxConcurrency: 4);

        PreviewCount = residuals.Count;
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        if (SelectedProgram is null)
        {
            return;
        }

        bool confirmed = await _confirmation.ConfirmAsync(
            _localization[LocalizationKeys.CommonUninstall],
            _localization.Format(LocalizationKeys.UninstallConfirm, SelectedProgram.DisplayName),
            _localization[LocalizationKeys.CommonConfirm],
            _localization[LocalizationKeys.CommonCancel]);

        if (!confirmed)
        {
            return;
        }

        UninstallReport report = await _engine.RunAsync(SelectedProgram.Record, BuildOptions(), maxConcurrency: 4);

        if (report.Succeeded)
        {
            _messenger.ShowSuccess(_localization.Format(LocalizationKeys.UninstallDone, SelectedProgram.DisplayName));
        }
        else
        {
            _messenger.ShowError(_localization.Format(LocalizationKeys.UninstallFailed, SelectedProgram.DisplayName));
        }
    }
}
