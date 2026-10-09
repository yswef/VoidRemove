using Microsoft.UI.Xaml.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>WinUI page for MonitorViewModel; logic lives in the shared Core view model.</summary>
public sealed partial class MonitorPage : Page
{
    public MonitorPage(MonitorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
