using Microsoft.UI.Xaml.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>WinUI page for ActionCenterViewModel; logic lives in the shared Core view model.</summary>
public sealed partial class ActionCenterPage : Page
{
    public ActionCenterPage(ActionCenterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
