using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Shared.Theming;

/// <summary>
/// Holds the active theme, resolves accent overrides and tells the front ends to repaint.
/// </summary>
/// <remarks>
/// The manager knows nothing about WPF or WinUI. Each front end subscribes to
/// <see cref="ThemeChanged"/> and maps <see cref="ThemeColors"/> onto its own brush
/// resources, which is what makes the two UIs switch themes at the same instant.
/// </remarks>
public sealed class ThemeManager : ObservableObject
{
    private readonly ILocalizationService? _localization;
    private readonly ILogger<ThemeManager> _logger;
    private ThemeDefinition _current;

    public ThemeManager(
        ILocalizationService? localization = null,
        AppTheme initialTheme = AppTheme.Dark,
        string? accentHex = null,
        ILogger<ThemeManager>? logger = null)
    {
        _localization = localization;
        _logger = logger ?? NullLogger<ThemeManager>.Instance;
        _current = Resolve(initialTheme, accentHex);
    }

    /// <summary>The active theme, with the accent override already folded in.</summary>
    public ThemeDefinition Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    /// <summary>Every theme the user can pick.</summary>
    public IReadOnlyList<ThemeDefinition> Available => ThemeCatalog.Themes;

    /// <summary>Localized name of the active theme.</summary>
    public string CurrentDisplayName => _localization?[Current.DisplayNameKey] ?? Current.Id.ToString();

    /// <summary>Raised after the theme or the accent changed.</summary>
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>Switches theme and accent in one step.</summary>
    public void Apply(AppTheme theme, string? accentHex = null) => Set(Resolve(theme, accentHex));

    /// <summary>Keeps the theme and only changes the accent color.</summary>
    public void ApplyAccent(string accentHex) => Set(Resolve(Current.Id, accentHex));

    /// <summary>Builds the effective theme for a theme id plus an optional <c>#RRGGBB</c> accent.</summary>
    public ThemeDefinition Resolve(AppTheme theme, string? accentHex)
    {
        ThemeDefinition definition = ThemeCatalog.Get(theme);

        if (!definition.AccentCustomizable || string.IsNullOrWhiteSpace(accentHex))
        {
            return definition;
        }

        if (!RgbaColor.TryFromHex(accentHex, out RgbaColor accent))
        {
            _logger.LogWarning("Accent color {Accent} is not valid; keeping the theme default.", accentHex);
            return definition;
        }

        return definition.WithAccent(accent);
    }

    private void Set(ThemeDefinition theme)
    {
        if (theme == Current)
        {
            return;
        }

        Current = theme;
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentDisplayName)));
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(theme));
    }
}
