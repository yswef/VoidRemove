using Microsoft.UI.Xaml.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>WinUI page for ExtensionsViewModel; logic lives in the shared Core view model.</summary>
public sealed partial class ExtensionsPage : Page
{
    public ExtensionsPage(ExtensionsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
