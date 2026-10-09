using VoidRemove.Shared.Theming;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class ThemeCatalogTests
{
    [Fact]
    public void AllFiveThemesAreRegistered()
    {
        Assert.Equal(5, ThemeCatalog.Themes.Count);

        foreach (AppTheme theme in Enum.GetValues<AppTheme>())
        {
            Assert.True(ThemeCatalog.IsKnown(theme));
            Assert.Equal(theme, ThemeCatalog.Get(theme).Id);
        }
    }

    [Fact]
    public void ThemesHaveDistinctBackgrounds()
    {
        HashSet<string> backgrounds = ThemeCatalog.Themes
            .Select(theme => theme.Colors.WindowBackground.ToHex())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(ThemeCatalog.Themes.Count, backgrounds.Count);
    }

    [Fact]
    public void AmoledUsesPureBlack()
    {
        ThemeColors colors = ThemeCatalog.Amoled.Colors;

        Assert.Equal(new RgbaColor(0, 0, 0), colors.WindowBackground);
        Assert.Equal(new RgbaColor(0, 0, 0), colors.SidebarBackground);
    }

    [Fact]
    public void LightThemeIsFlaggedAsLight()
    {
        Assert.False(ThemeCatalog.Light.IsDark);
        Assert.True(ThemeCatalog.Light.Colors.TextPrimary.Luminance() < ThemeCatalog.Light.Colors.WindowBackground.Luminance());
    }

    [Fact]
    public void OnlyGlassAsksForTransparency()
    {
        Assert.True(ThemeCatalog.Glass.SupportsTransparency);
        Assert.False(ThemeCatalog.Dark.SupportsTransparency);
        Assert.False(ThemeCatalog.Amoled.SupportsTransparency);
    }

    [Fact]
    public void DefaultThemeIsDark()
    {
        Assert.Equal(AppTheme.Dark, ThemeCatalog.Default.Id);
    }

    [Fact]
    public void UnknownThemeFallsBackToTheDefault()
    {
        Assert.Equal(ThemeCatalog.Default, ThemeCatalog.Get((AppTheme)42));
    }

    [Fact]
    public void WithAccentDerivesHoverPressedAndForeground()
    {
        RgbaColor accent = RgbaColor.FromHex("#7B61FF");

        ThemeDefinition derived = ThemeCatalog.Dark.WithAccent(accent);

        Assert.Equal(accent, derived.Colors.Accent);
        Assert.NotEqual(derived.Colors.Accent, derived.Colors.AccentHover);
        Assert.NotEqual(derived.Colors.Accent, derived.Colors.AccentPressed);
        Assert.True(derived.Colors.AccentHover.Luminance() > derived.Colors.Accent.Luminance());
        Assert.True(derived.Colors.AccentPressed.Luminance() < derived.Colors.Accent.Luminance());
    }

    [Fact]
    public void WithAccentKeepsTheRestOfThePalette()
    {
        ThemeDefinition derived = ThemeCatalog.Dark.WithAccent(RgbaColor.FromHex("#7B61FF"));

        Assert.Equal(ThemeCatalog.Dark.Colors.WindowBackground, derived.Colors.WindowBackground);
        Assert.Equal(ThemeCatalog.Dark.Colors.TextPrimary, derived.Colors.TextPrimary);
        Assert.Equal(ThemeCatalog.Dark.Colors.Border, derived.Colors.Border);
        Assert.Equal(ThemeCatalog.Dark.Id, derived.Id);
    }

    [Fact]
    public void WithAccentIsANoOpForNonCustomizableThemes()
    {
        ThemeDefinition locked = ThemeCatalog.Dark with { AccentCustomizable = false };

        Assert.Same(locked, locked.WithAccent(RgbaColor.FromHex("#FF0000")));
    }

    [Fact]
    public void AccentThemeManagerResolvesInvalidHexToTheThemeDefault()
    {
        ThemeManager manager = new();

        Assert.Equal(ThemeCatalog.Dark.Colors.Accent, manager.Resolve(AppTheme.Dark, "not-a-color").Colors.Accent);
        Assert.Equal(RgbaColor.FromHex("#FF0000"), manager.Resolve(AppTheme.Dark, "#FF0000").Colors.Accent);
    }

    [Fact]
    public void ThemeManagerRaisesOneEventPerRealChange()
    {
        ThemeManager manager = new();

        int raised = 0;
        manager.ThemeChanged += (_, _) => raised++;

        manager.Apply(AppTheme.Neon);
        manager.Apply(AppTheme.Neon);
        manager.ApplyAccent("#FF0000");

        Assert.Equal(2, raised);
        Assert.Equal(AppTheme.Neon, manager.Current.Id);
        Assert.Equal(RgbaColor.FromHex("#FF0000"), manager.Current.Colors.Accent);
    }
}
