using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Core.History;
using VoidRemove.Core.Programs;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Core.ViewModels;

/// <summary>One internal recommendation; never an advertisement.</summary>
public sealed record ActionRecommendation(string Id, string Text);

/// <summary>Action Center: internal, dismissable recommendations derived from local state.</summary>
public sealed partial class ActionCenterViewModel : ObservableObject
{
    private readonly IProgramReader _reader;
    private readonly IUninstallHistoryStore _history;
    private readonly BundlewareCatalog _bundleware;
    private readonly IStatusMessenger _messenger;
    private readonly ILocalizationService _localization;

    private readonly HashSet<string> _dismissed = new(StringComparer.Ordinal);

    public ActionCenterViewModel(
        IProgramReader reader,
        IUninstallHistoryStore history,
        BundlewareCatalog bundleware,
        IStatusMessenger messenger,
        ILocalizationService localization)
    {
        _reader = reader;
        _history = history;
        _bundleware = bundleware;
        _messenger = messenger;
        _localization = localization;

        Recommendations = new ObservableCollection<ActionRecommendation>();
    }

    public ObservableCollection<ActionRecommendation> Recommendations { get; }

    public string Title => _localization[LocalizationKeys.NavActionCenter];

    [ObservableProperty]
    private ActionRecommendation? _selectedItem;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Recommendations.Clear();

        BundlewareCatalogData bundleData = _bundleware.Load(ProgramsViewModel.DataFilePath("bundleware.json"));

        foreach (ProgramRecord program in await _reader.ReadAsync())
        {
            if (_bundleware.IsBundleware(program.DisplayName, program.Publisher, bundleData))
            {
                Add($"bundleware.{program.KeyName}", _localization.Format(LocalizationKeys.ActionBundleware, program.DisplayName));
            }
        }

        foreach (UninstallReport report in await _history.ListAsync())
        {
            if (!report.Succeeded && !report.WasDryRun)
            {
                Add($"failed.{report.Id}", _localization.Format(LocalizationKeys.ActionFailedUninstall, report.ProgramName));
            }
        }
    }

    private void Add(string id, string text)
    {
        if (!_dismissed.Contains(id))
        {
            Recommendations.Add(new ActionRecommendation(id, text));
        }
    }

    [RelayCommand]
    private void DismissSelected()
    {
        if (SelectedItem is null)
        {
            return;
        }

        _dismissed.Add(SelectedItem.Id);
        Recommendations.Remove(SelectedItem);
    }

    [RelayCommand]
    private void FixAll()
    {
        int count = Recommendations.Count;
        Recommendations.Clear();
        _messenger.ShowSuccess(_localization.Format(LocalizationKeys.ResetDone, count));
    }
}
