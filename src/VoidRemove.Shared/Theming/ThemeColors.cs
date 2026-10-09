namespace VoidRemove.Shared.Theming;

/// <summary>
/// Every color a front end needs to paint the application.
/// </summary>
/// <remarks>
/// A record so themes can be derived with <c>with</c> expressions (see
/// <see cref="ThemeDefinition.WithAccent"/>) and compared by value in tests.
/// The defaults describe the built-in Dark theme.
/// </remarks>
public sealed record ThemeColors
{
    // ---- Chrome ----
    public RgbaColor WindowBackground { get; init; } = RgbaColor.FromHex("#0B0E14");
    public RgbaColor SidebarBackground { get; init; } = RgbaColor.FromHex("#090C11");
    public RgbaColor TitleBarBackground { get; init; } = RgbaColor.FromHex("#0B0E14");

    // ---- Surfaces ----
    public RgbaColor CardBackground { get; init; } = RgbaColor.FromHex("#121722");
    public RgbaColor ElevatedBackground { get; init; } = RgbaColor.FromHex("#171D2A");
    public RgbaColor InputBackground { get; init; } = RgbaColor.FromHex("#0F1420");
    public RgbaColor SelectionBackground { get; init; } = RgbaColor.FromHex("#222EE6A8");

    // ---- Lines ----
    public RgbaColor Border { get; init; } = RgbaColor.FromHex("#232B3B");
    public RgbaColor Divider { get; init; } = RgbaColor.FromHex("#1A2130");

    // ---- Text ----
    public RgbaColor TextPrimary { get; init; } = RgbaColor.FromHex("#E8ECF4");
    public RgbaColor TextSecondary { get; init; } = RgbaColor.FromHex("#98A2B8");
    public RgbaColor TextDisabled { get; init; } = RgbaColor.FromHex("#5B6478");

    // ---- Accent ----
    public RgbaColor Accent { get; init; } = RgbaColor.FromHex("#2EE6A8");
    public RgbaColor AccentHover { get; init; } = RgbaColor.FromHex("#5CEBBC");
    public RgbaColor AccentPressed { get; init; } = RgbaColor.FromHex("#25BF8C");
    public RgbaColor AccentForeground { get; init; } = RgbaColor.FromHex("#06120D");

    // ---- Semantic ----
    public RgbaColor Success { get; init; } = RgbaColor.FromHex("#35D07F");
    public RgbaColor Warning { get; init; } = RgbaColor.FromHex("#F5B840");
    public RgbaColor Danger { get; init; } = RgbaColor.FromHex("#FF5470");
    public RgbaColor Info { get; init; } = RgbaColor.FromHex("#4CC2FF");
    public RgbaColor Overlay { get; init; } = RgbaColor.FromHex("#8C000000");
}
