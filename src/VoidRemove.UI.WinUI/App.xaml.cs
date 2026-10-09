using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using VoidRemove.Core.Di;
using VoidRemove.Core.Hosting;
using VoidRemove.Core.Logging;
using VoidRemove.Core.Settings;
using VoidRemove.Core.Storage;
using VoidRemove.Shared;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Icons;
using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Theming;
using VoidRemove.Shared.ViewModels;
using VoidRemove.Core.ViewModels;
using VoidRemove.UI.WinUI.Services;
using VoidRemove.UI.WinUI.Views;

namespace VoidRemove.UI.WinUI;

/// <summary>
/// Composition root of the WinUI front end.
/// </summary>
/// <remarks>
/// Registers the exact same core services as the WPF app and only differs in the
/// implementations of the four shell contracts, which is what keeps the two front ends
/// behaving identically.
/// </remarks>
public partial class App : Application
{
    private ServiceProvider? _provider;
    private MainWindow? _window;
    private SingleInstanceGuard? _guard;
    private CancellationTokenSource? _ipcCancellation;
    private ILogger<App>? _logger;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    /// <summary>XamlRoot of the shell window, needed to host dialogs.</summary>
    public XamlRoot? ContentRoot => _window?.Content?.XamlRoot;

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppPaths paths = AppPaths.Default;
        Serilog.Core.Logger serilog = LogSetup.CreateSerilogLogger(paths);

        // A second launch hands its arguments to the running window and exits.
        _guard = new SingleInstanceGuard();
        if (!_guard.TryAcquire())
        {
            try
            {
                _guard.SendArgumentsAsync(Array.Empty<string>()).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // The first instance may already be shutting down.
            }

            _guard.Dispose();
            _guard = null;
            Exit();
            return;
        }

        ServiceCollection services = new();
        services.AddLogging(builder => builder.AddSerilog(serilog, dispose: true));
        services.AddVoidRemoveCore();

        services.AddSingleton<WinUIStatusMessenger>();
        services.AddSingleton<IStatusMessenger>(provider => provider.GetRequiredService<WinUIStatusMessenger>());
        services.AddSingleton<IExternalLauncher, WinUIExternalLauncher>();
        services.AddSingleton<IConfirmationDialog>(provider => new WinUIConfirmationDialog(() => ContentRoot));

        // The same section view models the WPF app uses; the views differ, the behavior does not.
        services.AddSingleton<ProgramsViewModel>();
        services.AddSingleton<TotalUninstallViewModel>();
        services.AddSingleton<ResetAppViewModel>();
        services.AddSingleton<HealthViewModel>();
        services.AddSingleton<MonitorViewModel>();
        services.AddSingleton<ExtensionsViewModel>();
        services.AddSingleton<WindowsAppsViewModel>();
        services.AddSingleton<ToolboxViewModel>();
        services.AddSingleton<ActionCenterViewModel>();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _logger = _provider.GetRequiredService<ILoggerFactory>().CreateLogger<App>();
        _logger.LogInformation("{Product} {Version} starting (WinUI).", AppInfo.Name, AppInfo.Version);

        ISettingsService settingsService = _provider.GetRequiredService<ISettingsService>();
        AppSettings settings = settingsService.LoadAsync().GetAwaiter().GetResult();

        ILocalizationService localization = _provider.GetRequiredService<ILocalizationService>();
        localization.SetLanguage(settings.Language);

        ThemeManager themes = _provider.GetRequiredService<ThemeManager>();
        themes.Apply(settings.Theme, settings.AccentColor);

        _window = new MainWindow(
            _provider.GetRequiredService<ShellViewModel>(),
            themes,
            _provider.GetRequiredService<WinUIStatusMessenger>(),
            _provider.GetRequiredService<HomeViewModel>(),
            _provider.GetRequiredService<SettingsViewModel>());

        RegisterSectionsAndPages(_provider.GetRequiredService<ShellViewModel>(), _window);

        _window.Activate();

        StartActivationListener();
    }

    /// <summary>
    /// Attaches the same section set the WPF app registers, pairing each sidebar entry with
    /// its WinUI page. Because the ids and view models match, the two shells behave alike.
    /// </summary>
    private void RegisterSectionsAndPages(ShellViewModel shell, MainWindow window)
    {
        ServiceProvider provider = _provider!;

        void Add(string groupId, string groupTitle, string id, string title, string icon, ObservableObject page_vm, UIElement page)
        {
            shell.RegisterSection(groupId, groupTitle, id, title, icon, page_vm);
            window.RegisterPage(id, page);
        }

        ProgramsViewModel programs = provider.GetRequiredService<ProgramsViewModel>();
        Add(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "programs", LocalizationKeys.NavPrograms, IconGeometry.Programs, programs, new ProgramsPage(programs));

        TotalUninstallViewModel total = provider.GetRequiredService<TotalUninstallViewModel>();
        Add(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "uninstall", LocalizationKeys.NavUninstall, IconGeometry.Delete, total, new TotalUninstallPage(total));

        ResetAppViewModel reset = provider.GetRequiredService<ResetAppViewModel>();
        Add(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "reset", LocalizationKeys.NavReset, IconGeometry.Reset, reset, new ResetAppPage(reset));

        HealthViewModel health = provider.GetRequiredService<HealthViewModel>();
        Add(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "health", LocalizationKeys.NavHealth, IconGeometry.Health, health, new HealthPage(health));

        MonitorViewModel monitor = provider.GetRequiredService<MonitorViewModel>();
        Add(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "monitor", LocalizationKeys.NavMonitor, IconGeometry.Monitor, monitor, new MonitorPage(monitor));

        ExtensionsViewModel extensions = provider.GetRequiredService<ExtensionsViewModel>();
        Add(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "extensions", LocalizationKeys.NavExtensions, IconGeometry.Extensions, extensions, new ExtensionsPage(extensions));

        WindowsAppsViewModel apps = provider.GetRequiredService<WindowsAppsViewModel>();
        Add(LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, "windowsApps", LocalizationKeys.NavWindowsApps, IconGeometry.WindowsApps, apps, new WindowsAppsPage(apps));

        ToolboxViewModel toolbox = provider.GetRequiredService<ToolboxViewModel>();
        Add(LocalizationKeys.NavGroupTools, LocalizationKeys.NavGroupTools, "toolbox", LocalizationKeys.NavToolbox, IconGeometry.Toolbox, toolbox, new ToolboxPage(toolbox));

        ActionCenterViewModel actions = provider.GetRequiredService<ActionCenterViewModel>();
        Add(LocalizationKeys.NavGroupTools, LocalizationKeys.NavGroupTools, "actionCenter", LocalizationKeys.NavActionCenter, IconGeometry.ActionCenter, actions, new ActionCenterPage(actions));
    }

    private void StartActivationListener()
    {
        if (_guard is not { } guard || _window is not { } window)
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
                        window.DispatcherQueue.TryEnqueue(window.ActivateFromSecondInstance);
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

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unhandled WinUI exception.");
        e.Handled = true;
    }
}
