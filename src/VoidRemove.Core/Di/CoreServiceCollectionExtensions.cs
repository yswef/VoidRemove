using Microsoft.Extensions.DependencyInjection;
using VoidRemove.Core.AppData;
using VoidRemove.Core.Backup;
using VoidRemove.Core.Diagnostics;
using VoidRemove.Core.Engine;
using VoidRemove.Core.History;
using VoidRemove.Core.Hosting;
using VoidRemove.Core.Monitor;
using VoidRemove.Core.Programs;
using VoidRemove.Core.Residual;
using VoidRemove.Core.Extensions;
using VoidRemove.Core.Health;
using VoidRemove.Core.Safety;
using VoidRemove.Core.Security;
using VoidRemove.Core.Shell;
using VoidRemove.Core.Settings;
using VoidRemove.Core.Storage;
using VoidRemove.Core.Toolbox;
using VoidRemove.Core.Uninstall;
using VoidRemove.Core.WindowsApps;
using VoidRemove.Shared;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Di;

namespace VoidRemove.Core.Di;

/// <summary>Startup configuration for the core services.</summary>
public sealed class CoreOptions
{
    /// <summary>Options forwarded to the shared layer (language, theme, accent).</summary>
    public SharedOptions Shared { get; init; } = new();

    /// <summary>Override the data root. Tests point this at a temporary folder.</summary>
    public AppPaths? Paths { get; set; }
}

/// <summary>Registers everything a front end needs from <c>VoidRemove.Core</c>.</summary>
public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds the shared layer plus paths, settings, secrets, system information and the
    /// single instance guard. Both the WPF and the WinUI app call exactly this, which is
    /// the reason they behave the same.
    /// </summary>
    public static IServiceCollection AddVoidRemoveCore(this IServiceCollection services, Action<CoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        CoreOptions options = new();
        configure?.Invoke(options);

        services.AddVoidRemoveShared(options.Shared);

        AppPaths paths = options.Paths ?? AppPaths.Default;
        services.AddSingleton(paths);
        services.AddSingleton<IDataLocations>(provider => provider.GetRequiredService<AppPaths>());

        services.AddSingleton<ISecretProtector>(_ =>
            OperatingSystem.IsWindows() ? new DpapiSecretProtector() : new PlainTextSecretProtector());

        services.AddSingleton<IKeyValueStore>(_ =>
            OperatingSystem.IsWindows() ? new RegistryKeyValueStore(AppInfo.RegistryRoot) : new NullKeyValueStore());

        services.AddSingleton<SettingsService>();
        services.AddSingleton<ISettingsService>(provider => provider.GetRequiredService<SettingsService>());
        services.AddSingleton<ISettingsController>(provider => provider.GetRequiredService<SettingsService>());

        services.AddSingleton<ISystemInformation, SystemInfoProvider>();
        services.AddSingleton<SingleInstanceGuard>();

        // Removal engine graph (phase 2).
        services.AddSingleton<IProgramReader, RegistryProgramReader>();
        services.AddSingleton<IResidualScanner, ResidualScanner>();
        services.AddSingleton<QuarantineService>();
        services.AddSingleton<RegistryBackupService>();
        services.AddSingleton<IUninstallHistoryStore, JsonUninstallHistoryStore>();
        services.AddSingleton<ExclusionList>();
        services.AddSingleton<IProcessRunner, WindowsProcessRunner>();
        services.AddSingleton<IRestorePointService, NullRestorePointService>();
        services.AddSingleton<UninstallEngine>();

        // Reset App + Install Monitor (phase 3).
        services.AddSingleton<ProfileCatalog>();
        services.AddSingleton<AppDataResolver>();
        services.AddSingleton<ResetEngine>();
        services.AddSingleton<ISnapshotProvider, WindowsSnapshotProvider>();
        services.AddSingleton<IInstallMonitorStore, JsonInstallMonitorStore>();
        services.AddSingleton<InstallMonitor>();

        // Health, extensions, Windows apps and toolbox (phase 4).
        services.AddSingleton<IVirusTotalClient, OfflineVirusTotalClient>();
        services.AddSingleton<HealthScanner>();
        services.AddSingleton<IBrowserExtensionScanner, WindowsBrowserExtensionScanner>();
        services.AddSingleton<IWindowsAppService, PowerShellWindowsAppService>();
        services.AddSingleton<BundlewareCatalog>();
        services.AddSingleton<StubbornCatalog>();
        services.AddSingleton<FileShredder>();
        services.AddSingleton<WindowsUpdatesService>();
        services.AddSingleton<ForceUninstallService>();

        // Shell integration (phase 7).
        services.AddSingleton<ExplorerContextMenuService>();

        return services;
    }
}
