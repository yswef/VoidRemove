using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Core.Engine;
using VoidRemove.Core.History;
using VoidRemove.Core.Programs;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Core.ViewModels;

/// <summary>A row in the programs list.</summary>
public sealed partial class ProgramItemViewModel : ObservableObject
{
    public ProgramItemViewModel(ProgramRecord record, bool isBundleware)
    {
        Record = record;
        IsBundleware = isBundleware;
    }

    public ProgramRecord Record { get; }

    public bool IsBundleware { get; }

    public string DisplayName => Record.DisplayName;

    public string Publisher => Record.Publisher;

    public string Version => Record.DisplayVersion;

    public string SizeText => Record.EstimatedSizeKb > 0 ? $"{Record.EstimatedSizeKb / 1024.0:0.#} MB" : string.Empty;

    public string Tags => string.Join(" · ", new[]
    {
        Record.Architecture == ProgramArchitecture.X86 ? "x86" : Record.Architecture == ProgramArchitecture.X64 ? "x64" : "",
        Record.ScopeLabel,
        IsBundleware ? "Bundleware" : "",
    }.Where(static tag => tag.Length > 0));
}

/// <summary>The Programs section: discovery, search, sub-tabs and removal.</summary>
public sealed partial class ProgramsViewModel : ObservableObject
{
    private readonly IProgramReader _reader;
    private readonly BundlewareCatalog _bundleware;
    private readonly UninstallEngine _engine;
    private readonly IConfirmationDialog _confirmation;
    private readonly IStatusMessenger _messenger;
    private readonly ILocalizationService _localization;

    private IReadOnlyList<ProgramRecord> _all = Array.Empty<ProgramRecord>();

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private string _activeTab = "all";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ProgramItemViewModel? _selectedItem;

    public ProgramsViewModel(
        IProgramReader reader,
        BundlewareCatalog bundleware,
        UninstallEngine engine,
        IConfirmationDialog confirmation,
        IStatusMessenger messenger,
        ILocalizationService localization)
    {
        _reader = reader;
        _bundleware = bundleware;
        _engine = engine;
        _confirmation = confirmation;
        _messenger = messenger;
        _localization = localization;

        Programs = new ObservableCollection<ProgramItemViewModel>();
    }

    public ObservableCollection<ProgramItemViewModel> Programs { get; }

    public string Title => _localization[LocalizationKeys.NavPrograms];

    partial void OnSearchChanged(string value) => ApplyFilter();

    partial void OnActiveTabChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            _all = await _reader.ReadAsync();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SetTab(string tab) => ActiveTab = tab;

    [RelayCommand]
    private async Task UninstallSelectedAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        ProgramRecord record = SelectedItem.Record;

        bool confirmed = await _confirmation.ConfirmAsync(
            _localization[LocalizationKeys.CommonUninstall],
            _localization.Format(LocalizationKeys.UninstallConfirm, record.DisplayName),
            _localization[LocalizationKeys.CommonConfirm],
            _localization[LocalizationKeys.CommonCancel]);

        if (!confirmed)
        {
            return;
        }

        UninstallReport report = await _engine.RunAsync(record, new UninstallOptions(), maxConcurrency: 4);

        if (report.Succeeded)
        {
            _messenger.ShowSuccess(_localization.Format(LocalizationKeys.UninstallDone, record.DisplayName));
        }
        else
        {
            _messenger.ShowError(_localization.Format(LocalizationKeys.UninstallFailed, record.DisplayName));
        }

        await RefreshAsync();
    }

    private void ApplyFilter()
    {
        BundlewareCatalogData bundleData = _bundleware.Load(DataFilePath("bundleware.json"));

        IEnumerable<ProgramRecord> filtered = _all;

        if (!string.IsNullOrWhiteSpace(Search))
        {
            filtered = filtered.Where(p =>
                p.DisplayName.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                p.Publisher.Contains(Search, StringComparison.OrdinalIgnoreCase));
        }

        filtered = ActiveTab switch
        {
            "bundleware" => filtered.Where(p => _bundleware.IsBundleware(p.DisplayName, p.Publisher, bundleData)),
            "recent" => filtered.Where(p => p.InstallDate >= DateTime.UtcNow.AddDays(-30)),
            "large" => filtered.Where(p => p.EstimatedSizeKb > 102400),
            _ => filtered,
        };

        Programs.Clear();
        foreach (ProgramRecord record in filtered)
        {
            Programs.Add(new ProgramItemViewModel(record, _bundleware.IsBundleware(record.DisplayName, record.Publisher, bundleData)));
        }
    }

    internal static string DataFilePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "data", name);
}
