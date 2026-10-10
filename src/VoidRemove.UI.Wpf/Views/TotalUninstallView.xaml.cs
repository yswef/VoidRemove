using System.Windows;
using System.Windows.Controls;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.Wpf.Views;

/// <summary>Code-behind for TotalUninstallView; all logic lives in the bound view model.</summary>
public partial class TotalUninstallView : UserControl
{
    public TotalUninstallView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TotalUninstallViewModel viewModel && viewModel.Programs.Count == 0)
        {
            await viewModel.RefreshCommand.ExecuteAsync(null);
        }
    }
}
