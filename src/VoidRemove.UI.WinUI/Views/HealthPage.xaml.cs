using Microsoft.UI.Xaml.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>WinUI page for HealthViewModel; logic lives in the shared Core view model.</summary>
public sealed partial class HealthPage : Page
{
    public HealthPage(HealthViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
