using VoidRemove.Shared.Localization;

namespace VoidRemove.Shared.Theming;

/// <summary>The five built-in themes.</summary>
public static class ThemeCatalog
{
    /// <summary>Deep charcoal with a mint neon accent. The default.</summary>
    public static readonly ThemeDefinition Dark = new()
    {
        Id = AppTheme.Dark,
        DisplayNameKey = LocalizationKeys.ThemeDark,
        IsDark = true,
        Colors = new ThemeColors(),
    };

    /// <summary>Light neutral scheme.</summary>
    public static readonly ThemeDefinition Light = new()
    {
        Id = AppTheme.Light,
        DisplayNameKey = LocalizationKeys.ThemeLight,
        IsDark = false,
        Colors = new ThemeColors
        {
            WindowBackground = RgbaColor.FromHex("#F5F7FA"),
            SidebarBackground = RgbaColor.FromHex("#FFFFFF"),
            TitleBarBackground = RgbaColor.FromHex("#F5F7FA"),
            CardBackground = RgbaColor.FromHex("#FFFFFF"),
            ElevatedBackground = RgbaColor.FromHex("#FFFFFF"),
            InputBackground = RgbaColor.FromHex("#FFFFFF"),
            SelectionBackground = RgbaColor.FromHex("#1A0FA97C"),
            Border = RgbaColor.FromHex("#DCE1EA"),
            Divider = RgbaColor.FromHex("#E7EBF2"),
            TextPrimary = RgbaColor.FromHex("#14181F"),
            TextSecondary = RgbaColor.FromHex("#55607A"),
            TextDisabled = RgbaColor.FromHex("#9AA3B5"),
            Accent = RgbaColor.FromHex("#0FA97C"),
            AccentHover = RgbaColor.FromHex("#12C08C"),
            AccentPressed = RgbaColor.FromHex("#0C8A65"),
            AccentForeground = RgbaColor.FromHex("#FFFFFF"),
            Success = RgbaColor.FromHex("#128A55"),
            Warning = RgbaColor.FromHex("#B57500"),
            Danger = RgbaColor.FromHex("#D0304C"),
            Info = RgbaColor.FromHex("#0B72B8"),
            Overlay = RgbaColor.FromHex("#5A0B1220"),
        },
    };

    /// <summary>Pure black for OLED panels; the accent stays mint.</summary>
    public static readonly ThemeDefinition Amoled = new()
    {
        Id = AppTheme.Amoled,
        DisplayNameKey = LocalizationKeys.ThemeAmoled,
        IsDark = true,
        Colors = new ThemeColors
        {
            WindowBackground = RgbaColor.FromHex("#000000"),
            SidebarBackground = RgbaColor.FromHex("#000000"),
            TitleBarBackground = RgbaColor.FromHex("#000000"),
            CardBackground = RgbaColor.FromHex("#0A0A0A"),
            ElevatedBackground = RgbaColor.FromHex("#101010"),
            InputBackground = RgbaColor.FromHex("#060606"),
            SelectionBackground = RgbaColor.FromHex("#282EE6A8"),
            Border = RgbaColor.FromHex("#1E1E1E"),
            Divider = RgbaColor.FromHex("#141414"),
            TextPrimary = RgbaColor.FromHex("#F2F2F2"),
            TextSecondary = RgbaColor.FromHex("#9A9A9A"),
            TextDisabled = RgbaColor.FromHex("#555555"),
            Accent = RgbaColor.FromHex("#2EE6A8"),
            AccentHover = RgbaColor.FromHex("#5CEBBC"),
            AccentPressed = RgbaColor.FromHex("#25BF8C"),
            AccentForeground = RgbaColor.FromHex("#00150E"),
            Success = RgbaColor.FromHex("#3BD98A"),
            Warning = RgbaColor.FromHex("#FFC24D"),
            Danger = RgbaColor.FromHex("#FF6B81"),
            Info = RgbaColor.FromHex("#62C8FF"),
            Overlay = RgbaColor.FromHex("#AA000000"),
        },
    };

    /// <summary>Near black with a violet accent and higher contrast text.</summary>
    public static readonly ThemeDefinition Neon = new()
    {
        Id = AppTheme.Neon,
        DisplayNameKey = LocalizationKeys.ThemeNeon,
        IsDark = true,
        Colors = new ThemeColors
        {
            WindowBackground = RgbaColor.FromHex("#05060A"),
            SidebarBackground = RgbaColor.FromHex("#070912"),
            TitleBarBackground = RgbaColor.FromHex("#05060A"),
            CardBackground = RgbaColor.FromHex("#0D1120"),
            ElevatedBackground = RgbaColor.FromHex("#121830"),
            InputBackground = RgbaColor.FromHex("#0A0E1C"),
            SelectionBackground = RgbaColor.FromHex("#2E7B61FF"),
            Border = RgbaColor.FromHex("#2A2F5C"),
            Divider = RgbaColor.FromHex("#1B2040"),
            TextPrimary = RgbaColor.FromHex("#F2F5FF"),
            TextSecondary = RgbaColor.FromHex("#A7B0D8"),
            TextDisabled = RgbaColor.FromHex("#5A6290"),
            Accent = RgbaColor.FromHex("#7B61FF"),
            AccentHover = RgbaColor.FromHex("#9680FF"),
            AccentPressed = RgbaColor.FromHex("#634CD6"),
            AccentForeground = RgbaColor.FromHex("#FFFFFF"),
            Success = RgbaColor.FromHex("#00E5A0"),
            Warning = RgbaColor.FromHex("#FFD166"),
            Danger = RgbaColor.FromHex("#FF4D6D"),
            Info = RgbaColor.FromHex("#38BDF8"),
            Overlay = RgbaColor.FromHex("#A0000000"),
        },
    };

    /// <summary>Translucent surfaces; the front end asks Windows for a Mica backdrop.</summary>
    public static readonly ThemeDefinition Glass = new()
    {
        Id = AppTheme.Glass,
        DisplayNameKey = LocalizationKeys.ThemeGlass,
        IsDark = true,
        SupportsTransparency = true,
        CornerRadius = 12,
        Colors = new ThemeColors
        {
            WindowBackground = RgbaColor.FromHex("#C810131B"),
            SidebarBackground = RgbaColor.FromHex("#960E121A"),
            TitleBarBackground = RgbaColor.FromHex("#7810131B"),
            CardBackground = RgbaColor.FromHex("#A51A2030"),
            ElevatedBackground = RgbaColor.FromHex("#BE212A3D"),
            InputBackground = RgbaColor.FromHex("#AA141A26"),
            SelectionBackground = RgbaColor.FromHex("#2835E0D0"),
            Border = RgbaColor.FromHex("#1AFFFFFF"),
            Divider = RgbaColor.FromHex("#10FFFFFF"),
            TextPrimary = RgbaColor.FromHex("#EEF2FA"),
            TextSecondary = RgbaColor.FromHex("#A9B2C6"),
            TextDisabled = RgbaColor.FromHex("#66708A"),
            Accent = RgbaColor.FromHex("#35E0D0"),
            AccentHover = RgbaColor.FromHex("#5FE9DC"),
            AccentPressed = RgbaColor.FromHex("#28B9AA"),
            AccentForeground = RgbaColor.FromHex("#04211E"),
            Success = RgbaColor.FromHex("#4ADE80"),
            Warning = RgbaColor.FromHex("#FBBF24"),
            Danger = RgbaColor.FromHex("#FB7185"),
            Info = RgbaColor.FromHex("#60A5FA"),
            Overlay = RgbaColor.FromHex("#78000000"),
        },
    };

    private static readonly IReadOnlyDictionary<AppTheme, ThemeDefinition> Lookup =
        new Dictionary<AppTheme, ThemeDefinition>
        {
            [AppTheme.Dark] = Dark,
            [AppTheme.Light] = Light,
            [AppTheme.Amoled] = Amoled,
            [AppTheme.Neon] = Neon,
            [AppTheme.Glass] = Glass,
        };

    /// <summary>All themes, in the order the picker shows them.</summary>
    public static IReadOnlyList<ThemeDefinition> Themes { get; } = new[] { Dark, Light, Amoled, Neon, Glass };

    /// <summary>The theme used when nothing is configured.</summary>
    public static ThemeDefinition Default => Dark;

    /// <summary>Looks a theme up; unknown values fall back to <see cref="Default"/>.</summary>
    public static ThemeDefinition Get(AppTheme theme) =>
        Lookup.TryGetValue(theme, out ThemeDefinition? definition) ? definition : Default;

    /// <summary>True when <paramref name="theme"/> is one of the built-in themes.</summary>
    public static bool IsKnown(AppTheme theme) => Lookup.ContainsKey(theme);
}
