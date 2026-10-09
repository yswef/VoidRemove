using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Core.Health;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Core.ViewModels;

/// <summary>Software Health: scan and group findings by category.</summary>
public sealed partial class HealthViewModel : ObservableObject
{
    private readonly HealthScanner _scanner;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private long _reclaimableBytes;

    public HealthViewModel(HealthScanner scanner, ILocalizationService localization)
    {
        _scanner = scanner;
        _localization = localization;

        Findings = new ObservableCollection<HealthFinding>();
    }

    public ObservableCollection<HealthFinding> Findings { get; }

    public string Title => _localization[LocalizationKeys.NavHealth];

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsScanning = true;
        try
        {
            HealthReport report = await _scanner.ScanAsync(new HealthScanOptions());

            Findings.Clear();
            foreach (HealthFinding finding in report.Findings)
            {
                Findings.Add(finding);
            }

            ReclaimableBytes = report.ReclaimableBytes;
            StatusText = report.IsHealthy
                ? _localization[LocalizationKeys.HealthHealthy]
                : $"{Findings.Count} · {ReclaimableBytes / 1024.0 / 1024.0:0.#} MB";
        }
        finally
        {
            IsScanning = false;
        }
    }
}
