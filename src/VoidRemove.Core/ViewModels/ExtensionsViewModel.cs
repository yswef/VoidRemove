using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Core.Extensions;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Core.ViewModels;

/// <summary>Browser Extensions: trusted vs suspicious, individual and bulk removal.</summary>
public sealed partial class ExtensionsViewModel : ObservableObject
{
    private readonly IBrowserExtensionScanner _scanner;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private BrowserExtension? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    public ExtensionsViewModel(IBrowserExtensionScanner scanner, ILocalizationService localization)
    {
        _scanner = scanner;
        _localization = localization;

        Extensions = new ObservableCollection<BrowserExtension>();
    }

    public ObservableCollection<BrowserExtension> Extensions { get; }

    public string Title => _localization[LocalizationKeys.NavExtensions];

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            Extensions.Clear();
            foreach (BrowserExtension extension in await _scanner.ScanAsync())
            {
                Extensions.Add(extension);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem is null)
        {
            return;
        }

        RemoveOne(SelectedItem);
        Extensions.Remove(SelectedItem);
    }

    [RelayCommand]
    private void RemoveSuspicious()
    {
        foreach (BrowserExtension extension in Extensions.Where(static e => !e.IsTrusted).ToList())
        {
            RemoveOne(extension);
            Extensions.Remove(extension);
        }
    }

    private static void RemoveOne(BrowserExtension extension)
    {
        try
        {
            if (Directory.Exists(extension.SourcePath))
            {
                Directory.Delete(extension.SourcePath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
