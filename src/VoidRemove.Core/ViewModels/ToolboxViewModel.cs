using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Core.Toolbox;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Core.ViewModels;

/// <summary>Toolbox: File Shredder, Stubborn Remover, Windows Updates uninstall, Force Uninstall.</summary>
public sealed partial class ToolboxViewModel : ObservableObject
{
    private readonly FileShredder _shredder;
    private readonly StubbornCatalog _stubborn;
    private readonly WindowsUpdatesService _updates;
    private readonly ForceUninstallService _force;
    private readonly IStatusMessenger _messenger;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private StubbornProgram? _selectedStubborn;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _shredPath = string.Empty;

    public ToolboxViewModel(
        FileShredder shredder,
        StubbornCatalog stubborn,
        WindowsUpdatesService updates,
        ForceUninstallService force,
        IStatusMessenger messenger,
        ILocalizationService localization)
    {
        _shredder = shredder;
        _stubborn = stubborn;
        _updates = updates;
        _force = force;
        _messenger = messenger;
        _localization = localization;

        StubbornPrograms = new ObservableCollection<StubbornProgram>();
        Updates = new ObservableCollection<WindowsUpdate>();
    }

    public ObservableCollection<StubbornProgram> StubbornPrograms { get; }

    public ObservableCollection<WindowsUpdate> Updates { get; }

    public string Title => _localization[LocalizationKeys.NavToolbox];

    [RelayCommand]
    private void LoadStubborn()
    {
        StubbornPrograms.Clear();
        foreach (StubbornProgram program in _stubborn.Load(ProgramsViewModel.DataFilePath("stubborn.json")))
        {
            StubbornPrograms.Add(program);
        }
    }

    [RelayCommand]
    private async Task LoadUpdatesAsync()
    {
        IsLoading = true;
        try
        {
            Updates.Clear();
            foreach (WindowsUpdate update in await _updates.ListAsync())
            {
                Updates.Add(update);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task ShredAsync() => ShredFileAsync(ShredPath);

    /// <summary>Shreds a file chosen by the view; the irreversible warning is shown first.</summary>
    [RelayCommand]
    private async Task ShredFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        bool ok = await _shredder.ShredAsync(path);
        string name = System.IO.Path.GetFileName(path);
        if (ok)
        {
            _messenger.ShowSuccess(_localization.Format(LocalizationKeys.UninstallDone, name));
        }
        else
        {
            _messenger.ShowError(_localization.Format(LocalizationKeys.UninstallFailed, name));
        }
    }

    [RelayCommand]
    private async Task ForceRemoveSelectedAsync()
    {
        if (SelectedStubborn is null)
        {
            return;
        }

        StubbornProgram program = SelectedStubborn;
        await _force.ForceAsync(program.Processes, program.UninstallExecutable, program.UninstallArguments);
        _messenger.ShowSuccess(_localization.Format(LocalizationKeys.UninstallDone, program.DisplayName));
    }
}
