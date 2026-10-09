using Microsoft.UI.Xaml.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>WinUI page for WindowsAppsViewModel; logic lives in the shared Core view model.</summary>
public sealed partial class WindowsAppsPage : Page
{
    public WindowsAppsPage(WindowsAppsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
