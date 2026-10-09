namespace VoidRemove.Shared.Icons;

/// <summary>
/// VoidRemove's own vector icon set.
/// </summary>
/// <remarks>
/// Every path is drawn on a 24x24 grid and is meant to be stroked, not filled, with a
/// round cap and a 1.6 unit pen:
/// <list type="bullet">
///   <item><description>WPF: <c>&lt;Path Data="{Binding Icon}" Stroke="..." StrokeThickness="1.6"/&gt;</c></description></item>
///   <item><description>WinUI: <c>&lt;Path Data="{x:Bind Icon}" Stroke="..." StrokeThickness="1.6"/&gt;</c></description></item>
/// </list>
/// Drawing the icons ourselves keeps both front ends pixel identical and avoids depending
/// on an icon font being installed.
/// </remarks>
public static class IconGeometry
{
    /// <summary>Grid size every path is authored against.</summary>
    public const double GridSize = 24;

    /// <summary>Stroke width that looks right at 16-20px.</summary>
    public const double StrokeWidth = 1.6;

    // ---- Navigation ----

    /// <summary>House outline.</summary>
    public const string Home = "M3.5 10.8 L12 3.8 L20.5 10.8 V20 H14.4 V14.6 H9.6 V20 H3.5 Z";

    /// <summary>Four tiles: the installed programs grid.</summary>
    public const string Programs = "M4 4.5 H9.5 V10 H4 Z M14.5 4.5 H20 V10 H14.5 Z M4 14 H9.5 V19.5 H4 Z M14.5 14 H20 V19.5 H14.5 Z";

    /// <summary>Shield with a check: software health.</summary>
    public const string Health = "M12 3.2 L19.5 6 V12.2 C19.5 16.8 16.3 19.8 12 21 C7.7 19.8 4.5 16.8 4.5 12.2 V6 Z M8.4 12.1 L11 14.7 L15.8 9.6";

    /// <summary>Radar rings: install monitor.</summary>
    public const string Monitor = "M12 4.4 A7.6 7.6 0 1 1 11.99 4.4 Z M12 8.6 A3.4 3.4 0 1 1 11.99 8.6 Z M12 11.4 V12.6";

    /// <summary>Brick with a tab: browser extensions.</summary>
    public const string Extensions = "M4.5 8.6 H19.5 V19 H4.5 Z M9 8.6 V5.8 H15 V8.6 M4.5 13.4 H19.5";

    /// <summary>Window with a divider: Windows apps.</summary>
    public const string WindowsApps = "M3.5 5 H20.5 V19 H3.5 Z M3.5 9.4 H20.5 M9.4 9.4 V19";

    /// <summary>Bolt: action center.</summary>
    public const string ActionCenter = "M13.4 3 L6.6 13.2 H10.9 L9.9 21 L17.4 10.3 H12.6 Z";

    /// <summary>Toolbox: toolbox section.</summary>
    public const string Toolbox = "M3.5 8.6 H20.5 V19.4 H3.5 Z M9 8.6 V5.4 H15 V8.6 M3.5 13.4 H20.5 M10.8 12.2 H13.2 V14.6 H10.8 Z";

    /// <summary>Sliders: settings.</summary>
    public const string Settings = "M4 7.2 H20 M4 12 H20 M4 16.8 H20 M9.2 5 V9.4 M15.4 9.8 V14.2 M7.4 14.6 V19";

    // ---- Actions ----

    /// <summary>Magnifier.</summary>
    public const string Search = "M10.6 4.4 A6.2 6.2 0 1 1 10.59 4.4 Z M15.2 15.2 L20 20";

    /// <summary>Circular arrow: refresh.</summary>
    public const string Refresh = "M19.6 12 A7.6 7.6 0 1 1 16.6 5.9 M20 4.2 V9 H15.2";

    /// <summary>Trash can: delete.</summary>
    public const string Delete = "M4.8 7.2 H19.2 M9.4 7.2 V4.8 H14.6 V7.2 M6.8 7.2 L7.9 20 H16.1 L17.2 7.2 M10.4 10.4 V16.8 M13.6 10.4 V16.8";

    /// <summary>Counter clockwise arrow: reset app data.</summary>
    public const string Reset = "M4.4 12 A7.6 7.6 0 1 0 7.4 5.9 M4 4.2 V9 H8.8";

    /// <summary>Folder.</summary>
    public const string Folder = "M3.5 6.2 H9.2 L11.4 9 H20.5 V18.6 H3.5 Z";

    /// <summary>Info circle.</summary>
    public const string Info = "M12 4.2 A7.8 7.8 0 1 1 11.99 4.2 Z M12 10.8 V16.6 M12 7.4 V8.2";

    /// <summary>Warning triangle.</summary>
    public const string Warning = "M12 4.2 L20.8 19.6 H3.2 Z M12 9.6 V14 M12 16.4 V17.2";

    /// <summary>Check mark.</summary>
    public const string Check = "M5 12.4 L9.8 17.2 L19 6.8";

    // ---- Title bar ----

    /// <summary>Minimize dash.</summary>
    public const string Minimize = "M6 12 H18";

    /// <summary>Maximize square.</summary>
    public const string Maximize = "M6.2 6.2 H17.8 V17.8 H6.2 Z";

    /// <summary>Restore: two overlapping squares.</summary>
    public const string Restore = "M8.2 8.2 H18.6 V18.6 H8.2 Z M5.4 15.4 V5.4 H15.4";

    /// <summary>Close cross.</summary>
    public const string Close = "M6.4 6.4 L17.6 17.6 M17.6 6.4 L6.4 17.6";

    /// <summary>Logo mark, used on the splash screen and in the sidebar header.</summary>
    public const string Logo = "M12 3.4 A8.6 8.6 0 1 1 11.99 3.4 Z M12 8.4 A3.6 3.6 0 1 1 11.99 8.4 Z M4.6 19.4 L9 15 M19.4 19.4 L15 15";
}
