using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VoidRemove.Shared.Theming;
using VoidRemove.Shared.ViewModels;
using VoidRemove.UI.WinUI.Services;
using VoidRemove.UI.WinUI.Theming;
using VoidRemove.UI.WinUI.Views;

namespace VoidRemove.UI.WinUI;

/// <summary>
/// The shell window: extended title bar, sidebar navigation, content host and the toast.
/// All navigation state lives in the shared <see cref="ShellViewModel"/>.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;
    private readonly ThemeManager _themes;
    private readonly WinUIStatusMessenger _messenger;
    private readonly HomePage _homePage;
    private readonly SettingsPage _settingsPage;
    private readonly Dictionary<string, UIElement> _pages = new(StringComparer.Ordinal);

    public MainWindow(
        ShellViewModel shell,
        ThemeManager themes,
        WinUIStatusMessenger messenger,
        HomeViewModel home,
        SettingsViewModel settings)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(themes);
        ArgumentNullException.ThrowIfNull(messenger);

        InitializeComponent();

        _shell = shell;
        _themes = themes;
        _messenger = messenger;

        RootGrid.DataContext = new WinUIShellContext(shell);

        _homePage = new HomePage(home);
        _settingsPage = new SettingsPage(settings);
        _pages[ShellViewModel.HomeId] = _homePage;
        _pages[ShellViewModel.SettingsId] = _settingsPage;
        ContentHost.Content = _homePage;

        Title = shell.WindowTitle;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _themes.ThemeChanged += OnThemeChanged;
        _messenger.ToastRequested += OnToastRequested;

        ApplyTheme(themes.Current);
        NavList.SelectedItem = shell.SelectedItem;
    }

    /// <summary>Brings the window to the front. Used when a second instance is launched.</summary>
    public void ActivateFromSecondInstance()
    {
        Activate();
    }

    /// <summary>Maps a sidebar id to the page shown for it. The app registers its sections.</summary>
    public void RegisterPage(string id, UIElement page) => _pages[id] = page;

    private void OnNavSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is NavigationItem item)
        {
            _shell.Select(item.Id);
        }

        string id = _shell.SelectedItem?.Id ?? ShellViewModel.HomeId;
        ContentHost.Content = _pages.TryGetValue(id, out UIElement? page) ? page : _homePage;
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e) =>
        DispatcherQueue.TryEnqueue(() => ApplyTheme(e.Theme));

    private void ApplyTheme(ThemeDefinition theme)
    {
        // Newly created pages read the dictionary; the shell surfaces are set directly
        // because StaticResource does not re-resolve after the fact.
        WinUIThemeApplier.Apply(theme, Application.Current.Resources);

        RootGrid.Background = WinUIThemeApplier.Brush(theme.Colors.WindowBackground);
        SidebarPanel.Background = WinUIThemeApplier.Brush(theme.Colors.SidebarBackground);
        AppTitleBar.Background = WinUIThemeApplier.Brush(theme.Colors.TitleBarBackground);
        RootGrid.RequestedTheme = theme.IsDark ? ElementTheme.Dark : ElementTheme.Light;
    }

    private void OnToastRequested(object? sender, ToastEventArgs e) =>
        DispatcherQueue.TryEnqueue(() => ShowToast(e));

    private void ShowToast(ToastEventArgs e)
    {
        ToastBar.Severity = e.Kind switch
        {
            ToastKind.Success => InfoBarSeverity.Success,
            ToastKind.Warning => InfoBarSeverity.Warning,
            ToastKind.Error => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational,
        };

        ToastBar.Title = string.Empty;
        ToastBar.Message = e.Message;
        ToastBar.IsOpen = true;
    }
}
