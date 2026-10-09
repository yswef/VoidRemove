using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using VoidRemove.Shared.ViewModels;

namespace VoidRemove.UI.WinUI.Views;

/// <summary>Home dashboard. All state comes from the shared <c>HomeViewModel</c>.</summary>
public sealed partial class HomePage : Page
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => UpdateStatusDot();

    private void UpdateStatusDot() =>
        AdminDot.Fill = ResolveBrush(_viewModel.IsAdministrator ? "Brush.Success" : "Brush.Warning");

    private static Brush ResolveBrush(string key) =>
        Application.Current.Resources.TryGetValue(key, out object? value) && value is Brush brush
            ? brush
            : new SolidColorBrush(Microsoft.UI.Colors.Gray);
}
