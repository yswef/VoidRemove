namespace VoidRemove.Shared.Theming;

/// <summary>The built-in color schemes. Every value maps to a <see cref="ThemeDefinition"/>.</summary>
public enum AppTheme
{
    /// <summary>Deep charcoal with a neon accent. The default.</summary>
    Dark = 0,

    /// <summary>Light neutral scheme for bright rooms.</summary>
    Light = 1,

    /// <summary>Pure black background for OLED panels.</summary>
    Amoled = 2,

    /// <summary>High contrast dark scheme with a saturated accent.</summary>
    Neon = 3,

    /// <summary>Translucent surfaces that let the Mica/Acrylic backdrop through.</summary>
    Glass = 4,
}
