using Microsoft.UI.Xaml.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>WinUI page for ProgramsViewModel; logic lives in the shared Core view model.</summary>
public sealed partial class ProgramsPage : Page
{
    public ProgramsPage(ProgramsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
