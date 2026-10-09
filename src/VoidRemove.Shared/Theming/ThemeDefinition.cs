using VoidRemove.Shared.Localization;

namespace VoidRemove.Shared.Theming;

/// <summary>A complete, named color scheme plus the metrics that go with it.</summary>
public sealed record ThemeDefinition
{
    public required AppTheme Id { get; init; }

    /// <summary>Localization key for the display name, e.g. <see cref="LocalizationKeys.ThemeDark"/>.</summary>
    public required string DisplayNameKey { get; init; }

    public required ThemeColors Colors { get; init; }

    /// <summary>True for schemes that are meant for a dark backdrop.</summary>
    public bool IsDark { get; init; } = true;

    /// <summary>True when the front end should ask the OS for a Mica/Acrylic backdrop.</summary>
    public bool SupportsTransparency { get; init; }

    /// <summary>False for schemes whose accent is part of the identity and must not change.</summary>
    public bool AccentCustomizable { get; init; } = true;

    /// <summary>Corner radius used by cards, inputs and dialogs.</summary>
    public double CornerRadius { get; init; } = 10;

    /// <summary>Width of the navigation sidebar in device independent pixels.</summary>
    public double SidebarWidth { get; init; } = 236;

    /// <summary>Height of the custom title bar in device independent pixels.</summary>
    public double TitleBarHeight { get; init; } = 40;

    /// <summary>
    /// Returns this theme with a user supplied accent, deriving the hover and pressed
    /// shades and a readable foreground automatically.
    /// </summary>
    public ThemeDefinition WithAccent(RgbaColor accent)
    {
        if (!AccentCustomizable)
        {
            return this;
        }

        ThemeColors colors = Colors with
        {
            Accent = accent,
            AccentHover = ColorMath.Lighten(accent, 0.14),
            AccentPressed = ColorMath.Darken(accent, 0.12),
            AccentForeground = accent.ReadableForeground(),
            SelectionBackground = accent.WithAlpha(IsDark ? (byte)38 : (byte)28),
        };

        return this with { Colors = colors };
    }
}
