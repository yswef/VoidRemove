using Microsoft.UI.Xaml.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>WinUI page for TotalUninstallViewModel; logic lives in the shared Core view model.</summary>
public sealed partial class TotalUninstallPage : Page
{
    public TotalUninstallPage(TotalUninstallViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
