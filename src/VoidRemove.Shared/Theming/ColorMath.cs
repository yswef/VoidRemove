namespace VoidRemove.Shared.Theming;

/// <summary>Small color helpers used to derive hover/pressed shades from an accent.</summary>
public static class ColorMath
{
    /// <summary>Mixes <paramref name="color"/> toward white by <paramref name="amount"/> (0..1).</summary>
    public static RgbaColor Lighten(RgbaColor color, double amount) => Mix(color, new RgbaColor(255, 255, 255, color.A), amount);

    /// <summary>Mixes <paramref name="color"/> toward black by <paramref name="amount"/> (0..1).</summary>
    public static RgbaColor Darken(RgbaColor color, double amount) => Mix(color, new RgbaColor(0, 0, 0, color.A), amount);

    /// <summary>Linear interpolation between two colors. <paramref name="amount"/> is clamped to 0..1.</summary>
    public static RgbaColor Mix(RgbaColor from, RgbaColor to, double amount)
    {
        double t = Math.Clamp(amount, 0d, 1d);
        return new RgbaColor(
            Blend(from.R, to.R, t),
            Blend(from.G, to.G, t),
            Blend(from.B, to.B, t),
            Blend(from.A, to.A, t));
    }

    private static byte Blend(byte from, byte to, double t) =>
        (byte)Math.Clamp((int)Math.Round(from + ((to - from) * t)), 0, 255);
}
