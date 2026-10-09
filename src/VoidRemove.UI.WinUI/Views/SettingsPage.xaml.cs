using Microsoft.UI.Xaml.Controls;
using VoidRemove.Shared.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>Settings page. All state comes from the shared <c>SettingsViewModel</c>.</summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
