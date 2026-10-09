using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using VoidRemove.Core.Di;
using VoidRemove.Core.Hosting;
using VoidRemove.Core.Logging;
using VoidRemove.Core.Settings;
using VoidRemove.Core.Shell;
using VoidRemove.Core.Storage;
using VoidRemove.Shared;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Icons;
using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Theming;
using VoidRemove.Shared.ViewModels;
using VoidRemove.UI.Wpf.Services;
using VoidRemove.UI.Wpf.Theming;
using VoidRemove.Core.ViewModels;

namespace VoidRemove.UI.Wpf;

/// <summary>
/// Composition root of the WPF front end.
/// </summary>
/// <remarks>
/// Everything platform independent is registered by <c>AddVoidRemoveCore</c>; this class
/// only adds the WPF implementations of the shared contracts and drives the startup
/// sequence: single instance check, logging, settings, theme, splash, window.
/// </remarks>
public partial class App : Application
{
    private ServiceProvider? _provider;
    private SingleInstanceGuard? _guard;
    private CancellationTokenSource? _ipcCancellation;
    private ILogger<App>? _logger;
    private SplashWindow? _splash;
    private TrayIconService? _tray;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppPaths paths = AppPaths.Default;
        Serilog.Core.Logger serilog = LogSetup.CreateSerilogLogger(paths);

        // A second launch hands its arguments to the running window and exits.
        _guard = new SingleInstanceGuard();
        if (!_guard.TryAcquire())
        {
            try
            {
                _guard.SendArgumentsAsync(e.Args ?? Array.Empty<string>()).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // The first instance may already be shutting down; nothing useful to do.
            }

            _guard.Dispose();
            _guard = null;
            Shutdown();
            return;
        }

        ServiceCollection services = new();
        services.AddLogging(builder => builder.AddSerilog(serilog, dispose: true));
        services.AddVoidRemoveCore();

        services.AddSingleton<WpfStatusMessenger>();
        services.AddSingleton<IStatusMessenger>(provider => provider.GetRequiredService<WpfStatusMessenger>());
        services.AddSingleton<IExternalLauncher, WpfExternalLauncher>();
        services.AddSingleton<IConfirmationDialog, WpfConfirmationDialog>();

        // Section view models (phase 5); the views are resolved through MainWindow templates.
        services.AddSingleton<ProgramsViewModel>();
        services.AddSingleton<TotalUninstallViewModel>();
        services.AddSingleton<ResetAppViewModel>();
        services.AddSingleton<HealthViewModel>();
        services.AddSingleton<MonitorViewModel>();
        services.AddSingleton<ExtensionsViewModel>();
        services.AddSingleton<WindowsAppsViewModel>();
        services.AddSingleton<ToolboxViewModel>();
        services.AddSingleton<ActionCenterViewModel>();

        services.AddSingleton<MainWindow>();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _logger = _provider.GetRequiredService<ILoggerFactory>().CreateLogger<App>();
        _logger.LogInformation("{Product} {Version} starting.", AppInfo.Name, AppInfo.Version);

        // Settings are read before anything is drawn so the first frame already has the
        // right language and theme. The file is small; blocking here costs milliseconds.
        ISettingsService settingsService = _provider.GetRequiredService<ISettingsService>();
        AppSettings settings = settingsService.LoadAsync().GetAwaiter().GetResult();

        ILocalizationService localization = _provider.GetRequiredService<ILocalizationService>();
        localization.SetLanguage(settings.Language);

        ThemeManager themes = _provider.GetRequiredService<ThemeManager>();
        themes.Apply(settings.Theme, settings.AccentColor);
        WpfThemeApplier.Apply(themes.Current, Resources);

        RegisterSections(_provider.GetRequiredService<ShellViewModel>());

        _splash = new SplashWindow(localization);
        _splash.Show();

        MainWindow mainWindow = _provider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        ApplyContextMenu(settings);
        settingsService.SettingsApplied += OnSettingsApplied;

        StartTray(mainWindow);

        StartActivationListener();

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(CloseSplash));
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("{Product} shutting down.", AppInfo.Name);

        _ipcCancellation?.Cancel();
        _ipcCancellation?.Dispose();
        _tray?.Dispose();
        _guard?.Dispose();
        _provider?.Dispose();

        base.OnExit(e);
    }

    private void StartActivationListener()
    {
        if (_guard is not { } guard || MainWindow is not MainWindow mainWindow)
        {
            return;
        }

        _ipcCancellation = new CancellationTokenSource();
        CancellationToken token = _ipcCancellation.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await guard.ListenAsync(
                    arguments =>
                    {
                        _logger?.LogInformation("Activated by another instance with {Count} argument(s).", arguments.Count);
                        Dispatcher.BeginInvoke(new Action(mainWindow.ActivateFromSecondInstance));
                        return Task.CompletedTask;
                    },
                    token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }, token);
    }

    /// <summary>
    /// Attaches the phase-5 sections to the shared shell so the sidebar and the page map
    /// grow together. The ids are stable; the WinUI front end registers the same set.
    /// </summary>
    private void RegisterSections(ShellViewModel shell)
    {
        ServiceProvider provider = _provider!;

        shell.RegisterSection(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "programs", LocalizationKeys.NavPrograms, IconGeometry.Programs, provider.GetRequiredService<ProgramsViewModel>());
        shell.RegisterSection(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "uninstall", LocalizationKeys.NavUninstall, IconGeometry.Delete, provider.GetRequiredService<TotalUninstallViewModel>());
        shell.RegisterSection(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "reset", LocalizationKeys.NavReset, IconGeometry.Reset, provider.GetRequiredService<ResetAppViewModel>());
        shell.RegisterSection(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "health", LocalizationKeys.NavHealth, IconGeometry.Health, provider.GetRequiredService<HealthViewModel>());
        shell.RegisterSection(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "monitor", LocalizationKeys.NavMonitor, IconGeometry.Monitor, provider.GetRequiredService<MonitorViewModel>());
        shell.RegisterSection(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "extensions", LocalizationKeys.NavExtensions, IconGeometry.Extensions, provider.GetRequiredService<ExtensionsViewModel>());
        shell.RegisterSection(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "windowsApps", LocalizationKeys.NavWindowsApps, IconGeometry.WindowsApps, provider.GetRequiredService<WindowsAppsViewModel>());
        shell.RegisterSection("tools", LocalizationKeys.NavGroupTools, "toolbox", LocalizationKeys.NavToolbox, IconGeometry.Toolbox, provider.GetRequiredService<ToolboxViewModel>());
        shell.RegisterSection("tools", LocalizationKeys.NavGroupTools, "actionCenter", LocalizationKeys.NavActionCenter, IconGeometry.ActionCenter, provider.GetRequiredService<ActionCenterViewModel>());
    }

    private void StartTray(MainWindow mainWindow)
    {
        ILocalizationService localization = _provider!.GetRequiredService<ILocalizationService>();
        ISettingsService settingsService = _provider.GetRequiredService<ISettingsService>();

        _tray = new TrayIconService();
        _tray.Show(
            AppInfo.Name,
            AppInfo.Name,
            localization[LocalizationKeys.MonitorStart],
            settingsService.Snapshot.AutoMonitorEnabled,
            localization[LocalizationKeys.CommonClose],
            onOpen: () => Dispatcher.BeginInvoke(mainWindow.ActivateFromSecondInstance),
            onToggleMonitor: on => _ = settingsService.UpdateAsync(settings => settings.AutoMonitorEnabled = on),
            onExit: () => Dispatcher.BeginInvoke(Shutdown));
    }

    private void OnSettingsApplied(object? sender, SettingsSnapshot snapshot)
    {
        ApplyContextMenuFlags(snapshot.ExplorerContextMenuEnabled);
    }

    private void ApplyContextMenu(AppSettings settings) => ApplyContextMenuFlags(settings.ExplorerContextMenuEnabled);

    private void ApplyContextMenuFlags(bool enabled)
    {
        ExplorerContextMenuService contextMenu = _provider!.GetRequiredService<ExplorerContextMenuService>();
        ILocalizationService localization = _provider.GetRequiredService<ILocalizationService>();

        if (enabled)
        {
            contextMenu.Install(
                Environment.ProcessPath ?? string.Empty,
                localization[LocalizationKeys.NavUninstall],
                localization[LocalizationKeys.NavReset]);
        }
        else
        {
            contextMenu.Uninstall();
        }
    }

    private void CloseSplash()
    {
        _splash?.Close();
        _splash = null;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unhandled dispatcher exception.");
        MessageBox.Show(
            e.Exception.Message,
            AppInfo.Name,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) =>
        _logger?.LogCritical(e.ExceptionObject as Exception, "Unhandled application domain exception.");

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unobserved task exception.");
        e.SetObserved();
    }
}
