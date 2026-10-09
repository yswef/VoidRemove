using Microsoft.UI.Xaml.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>WinUI page for ToolboxViewModel; logic lives in the shared Core view model.</summary>
public sealed partial class ToolboxPage : Page
{
    public ToolboxPage(ToolboxViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
