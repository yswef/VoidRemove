using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VoidRemove.Shared.Icons;
using VoidRemove.Shared.Theming;
using VoidRemove.Shared.ViewModels;
using VoidRemove.UI.Wpf.Interop;
using VoidRemove.UI.Wpf.Services;
using VoidRemove.UI.Wpf.Theming;

namespace VoidRemove.UI.Wpf;

/// <summary>
/// The shell window: custom title bar, sidebar navigation, content host and the toast
/// overlay. All navigation state lives in the shared <see cref="ShellViewModel"/>.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(3.5);
    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(160));

    private readonly ThemeManager _themes;
    private readonly ShellViewModel _shell;
    private DispatcherTimer? _toastTimer;
    private IntPtr _hwnd;
    private bool _hotkeyRegistered;

    public MainWindow(ShellViewModel shell, ThemeManager themes, WpfStatusMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(themes);
        ArgumentNullException.ThrowIfNull(messenger);

        InitializeComponent();

        _themes = themes;
        _shell = shell;
        DataContext = shell;

        _themes.ThemeChanged += OnThemeChanged;
        messenger.ToastRequested += OnToastRequested;

        Loaded += OnLoaded;
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            _hotkeyRegistered = HotkeyHelper.Register(_hwnd);
            HotkeyHelper.Hook(source, OnGlobalHotKey);
        }
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        if (_hotkeyRegistered)
        {
            HotkeyHelper.Unregister(_hwnd);
        }

        base.OnClosed(e);
    }

    /// <summary>Ctrl+Alt+U from anywhere in the OS: surface the window on Programs.</summary>
    private void OnGlobalHotKey()
    {
        ActivateFromSecondInstance();
        _shell.Select("programs");
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        switch (e.Key)
        {
            case System.Windows.Input.Key.F5:
                InvokeCurrentRefresh();
                e.Handled = true;
                break;

            case System.Windows.Input.Key.Delete:
                if (_shell.CurrentPage is VoidRemove.Core.ViewModels.ProgramsViewModel programs)
                {
                    programs.UninstallSelectedCommand.Execute(null);
                    e.Handled = true;
                }

                break;

            case System.Windows.Input.Key.F when System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control:
                if (FindChild<System.Windows.Controls.TextBox>(this) is { } box)
                {
                    box.Focus();
                    box.SelectAll();
                    e.Handled = true;
                }

                break;
        }
    }

    private void InvokeCurrentRefresh()
    {
        switch (_shell.CurrentPage)
        {
            case VoidRemove.Core.ViewModels.ProgramsViewModel programs:
                programs.RefreshCommand.Execute(null);
                break;
            case VoidRemove.Core.ViewModels.HealthViewModel health:
                health.ScanCommand.Execute(null);
                break;
            case VoidRemove.Core.ViewModels.MonitorViewModel monitor:
                monitor.RefreshCommand.Execute(null);
                break;
            case VoidRemove.Core.ViewModels.ExtensionsViewModel extensions:
                extensions.RefreshCommand.Execute(null);
                break;
            case VoidRemove.Core.ViewModels.WindowsAppsViewModel apps:
                apps.RefreshCommand.Execute(null);
                break;
        }
    }

    private static T? FindChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
            System.Windows.DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            T? nested = FindChild<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    /// <summary>Brings the window to the front. Used when a second instance is launched.</summary>
    public void ActivateFromSecondInstance()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Focus();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DwmInterop.TryEnableRoundedCorners(this);
        WpfThemeApplier.Apply(_themes.Current, Application.Current.Resources);
        UpdateMaximizeGlyph();
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e) =>
        Dispatcher.Invoke(() => WpfThemeApplier.Apply(e.Theme, Application.Current.Resources));

    private void OnStateChanged(object? sender, EventArgs e) => UpdateMaximizeGlyph();

    private void UpdateMaximizeGlyph() =>
        MaximizeGlyph.Data = Geometry.Parse(WindowState == WindowState.Maximized
            ? IconGeometry.Restore
            : IconGeometry.Maximize);

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _toastTimer?.Stop();
        _themes.ThemeChanged -= OnThemeChanged;
    }

    private void OnToastRequested(object? sender, ToastEventArgs e) =>
        Dispatcher.BeginInvoke(() => ShowToast(e.Message, e.Kind), DispatcherPriority.Background);

    private void ShowToast(string message, ToastKind kind)
    {
        ToastText.Text = message;
        ToastDot.Fill = ResolveBrush(kind);
        ToastHost.Visibility = Visibility.Visible;
        ToastHost.BeginAnimation(OpacityProperty, new DoubleAnimation(1, FadeDuration));

        _toastTimer?.Stop();
        _toastTimer = new DispatcherTimer { Interval = ToastDuration };
        _toastTimer.Tick += OnToastElapsed;
        _toastTimer.Start();
    }

    private void OnToastElapsed(object? sender, EventArgs e)
    {
        _toastTimer?.Stop();

        DoubleAnimation fade = new(0, FadeDuration);
        fade.Completed += (_, _) => ToastHost.Visibility = Visibility.Collapsed;
        ToastHost.BeginAnimation(OpacityProperty, fade);
    }

    private static Brush ResolveBrush(ToastKind kind)
    {
        string key = kind switch
        {
            ToastKind.Success => "Brush.Success",
            ToastKind.Warning => "Brush.Warning",
            ToastKind.Error => "Brush.Danger",
            _ => "Brush.Info",
        };

        return Application.Current.TryFindResource(key) as Brush
               ?? Brushes.White;
    }
}
