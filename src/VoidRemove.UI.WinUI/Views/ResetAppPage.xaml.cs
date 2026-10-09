using Microsoft.UI.Xaml.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>WinUI page for ResetAppViewModel; logic lives in the shared Core view model.</summary>
public sealed partial class ResetAppPage : Page
{
    public ResetAppPage(ResetAppViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
