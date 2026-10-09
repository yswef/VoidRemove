using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Theming;
using VoidRemove.Shared.ViewModels;

namespace VoidRemove.Shared.Di;

/// <summary>Startup options for the shared services.</summary>
public sealed class SharedOptions
{
    /// <summary>Language to activate at startup. Null keeps the built-in default.</summary>
    public string? InitialLanguage { get; set; }

    /// <summary>Theme to activate at startup.</summary>
    public AppTheme InitialTheme { get; set; } = AppTheme.Dark;

    /// <summary>Accent override, e.g. <c>#2EE6A8</c>. Null keeps the theme accent.</summary>
    public string? InitialAccent { get; set; }
}

/// <summary>Registers localization, theming and the shared view models.</summary>
public static class SharedServiceCollectionExtensions
{
    /// <summary>
    /// Adds everything <c>VoidRemove.Shared</c> owns. Both front ends call
    /// <c>AddVoidRemoveCore</c>, which calls this in turn, so the two UIs always end up
    /// with the same singletons.
    /// </summary>
    public static IServiceCollection AddVoidRemoveShared(this IServiceCollection services, SharedOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        SharedOptions effective = options ?? new SharedOptions();

        services.AddSingleton<ILocalizationCatalogSource, EmbeddedJsonLocalizationCatalogSource>();

        services.AddSingleton<ILocalizationService>(provider => new LocalizationService(
            provider.GetRequiredService<ILocalizationCatalogSource>(),
            provider.GetService<ILogger<LocalizationService>>(),
            effective.InitialLanguage));

        services.AddSingleton(provider => new ThemeManager(
            provider.GetRequiredService<ILocalizationService>(),
            effective.InitialTheme,
            effective.InitialAccent,
            provider.GetService<ILogger<ThemeManager>>()));

        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ShellViewModel>();

        return services;
    }
}
