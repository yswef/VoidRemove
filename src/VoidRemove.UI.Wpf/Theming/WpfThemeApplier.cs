using System.Windows;
using System.Windows.Media;
using VoidRemove.Shared.Theming;

namespace VoidRemove.UI.Wpf.Theming;

/// <summary>
/// Maps the UI-neutral <see cref="ThemeColors"/> onto WPF resources.
/// </summary>
/// <remarks>
/// Every style in the application reads its colors with <c>DynamicResource</c>, so
/// replacing the values in <see cref="Application.Resources"/> repaints the whole window
/// instantly, without recreating a single control.
/// </remarks>
public static class WpfThemeApplier
{
    /// <summary>Writes a theme into a resource dictionary.</summary>
    public static void Apply(ThemeDefinition theme, ResourceDictionary target)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(target);

        ThemeColors colors = theme.Colors;

        target["Brush.Window"] = Brush(colors.WindowBackground);
        target["Brush.Sidebar"] = Brush(colors.SidebarBackground);
        target["Brush.TitleBar"] = Brush(colors.TitleBarBackground);
        target["Brush.Card"] = Brush(colors.CardBackground);
        target["Brush.Elevated"] = Brush(colors.ElevatedBackground);
        target["Brush.Input"] = Brush(colors.InputBackground);
        target["Brush.ControlHover"] = Brush(ColorMath.Mix(colors.ElevatedBackground, colors.Accent, 0.12));
        target["Brush.ControlPressed"] = Brush(ColorMath.Mix(colors.ElevatedBackground, colors.Accent, 0.22));
        target["Brush.Selection"] = Brush(colors.SelectionBackground);
        target["Brush.Border"] = Brush(colors.Border);
        target["Brush.Divider"] = Brush(colors.Divider);
        target["Brush.Text"] = Brush(colors.TextPrimary);
        target["Brush.TextSecondary"] = Brush(colors.TextSecondary);
        target["Brush.TextDisabled"] = Brush(colors.TextDisabled);
        target["Brush.Accent"] = Brush(colors.Accent);
        target["Brush.AccentHover"] = Brush(colors.AccentHover);
        target["Brush.AccentPressed"] = Brush(colors.AccentPressed);
        target["Brush.AccentForeground"] = Brush(colors.AccentForeground);
        target["Brush.Success"] = Brush(colors.Success);
        target["Brush.Warning"] = Brush(colors.Warning);
        target["Brush.Danger"] = Brush(colors.Danger);
        target["Brush.Info"] = Brush(colors.Info);
        target["Brush.Overlay"] = Brush(colors.Overlay);

        target["Radius.Corner"] = theme.CornerRadius;
        target["CornerRadius.Card"] = new CornerRadius(theme.CornerRadius);
        target["CornerRadius.Control"] = new CornerRadius(Math.Max(4, theme.CornerRadius - 2));
        target["Metric.SidebarWidth"] = theme.SidebarWidth;
        target["Metric.TitleBarHeight"] = theme.TitleBarHeight;
    }

    /// <summary>Converts a neutral color into a frozen WPF brush.</summary>
    public static SolidColorBrush Brush(RgbaColor color)
    {
        SolidColorBrush brush = new(Color.FromArgb(color.A, color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }
}
