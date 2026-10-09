using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Core.Monitor;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Core.ViewModels;

/// <summary>Install Monitor: logged installations, auto start/stop, manual sessions.</summary>
public sealed partial class MonitorViewModel : ObservableObject
{
    private readonly InstallMonitor _monitor;
    private readonly IInstallMonitorStore _store;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private bool _isAutoRunning;

    [ObservableProperty]
    private bool _isManualActive;

    public MonitorViewModel(InstallMonitor monitor, IInstallMonitorStore store, ILocalizationService localization)
    {
        _monitor = monitor;
        _store = store;
        _localization = localization;

        Logs = new ObservableCollection<InstallLog>();
    }

    public ObservableCollection<InstallLog> Logs { get; }

    public string Title => _localization[LocalizationKeys.NavMonitor];

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Logs.Clear();
        foreach (InstallLog log in await _store.ListAsync())
        {
            Logs.Add(log);
        }
    }

    [RelayCommand]
    private void ToggleAuto()
    {
        if (_monitor.IsAutoRunning)
        {
            _monitor.StopAuto();
        }
        else
        {
            _monitor.StartAuto();
        }

        IsAutoRunning = _monitor.IsAutoRunning;
    }

    [RelayCommand]
    private void BeginManual()
    {
        _monitor.BeginManual();
        IsManualActive = true;
    }

    [RelayCommand]
    private async Task EndManualAsync()
    {
        await _monitor.EndManualAsync("manual-setup");
        IsManualActive = false;
        await RefreshAsync();
    }
}
