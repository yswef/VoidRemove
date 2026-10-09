using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Core.WindowsApps;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Core.ViewModels;

/// <summary>Windows Apps: packaged apps with safety tags; system packages protected.</summary>
public sealed partial class WindowsAppsViewModel : ObservableObject
{
    private readonly IWindowsAppService _service;
    private readonly IConfirmationDialog _confirmation;
    private readonly IStatusMessenger _messenger;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private WindowsApp? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    public WindowsAppsViewModel(
        IWindowsAppService service,
        IConfirmationDialog confirmation,
        IStatusMessenger messenger,
        ILocalizationService localization)
    {
        _service = service;
        _confirmation = confirmation;
        _messenger = messenger;
        _localization = localization;

        Apps = new ObservableCollection<WindowsApp>();
    }

    public ObservableCollection<WindowsApp> Apps { get; }

    public string Title => _localization[LocalizationKeys.NavWindowsApps];

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            Apps.Clear();
            foreach (WindowsApp app in await _service.ListAsync())
            {
                Apps.Add(app);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        if (SelectedItem is null || SelectedItem.IsProtected)
        {
            return;
        }

        bool confirmed = await _confirmation.ConfirmAsync(
            _localization[LocalizationKeys.CommonRemove],
            SelectedItem.Name,
            _localization[LocalizationKeys.CommonConfirm],
            _localization[LocalizationKeys.CommonCancel]);

        if (!confirmed)
        {
            return;
        }

        bool ok = await _service.RemoveAsync(SelectedItem.PackageFamilyName);
        if (ok)
        {
            Apps.Remove(SelectedItem);
            _messenger.ShowSuccess(_localization.Format(LocalizationKeys.UninstallDone, SelectedItem.Name));
        }
        else
        {
            _messenger.ShowError(_localization.Format(LocalizationKeys.UninstallFailed, SelectedItem.Name));
        }
    }
}
