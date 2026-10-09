using System.Globalization;

namespace VoidRemove.Shared.Theming;

/// <summary>
/// A UI-framework-neutral ARGB color.
/// </summary>
/// <remarks>
/// <c>VoidRemove.Shared</c> must not reference WPF or WinUI types, so themes are expressed
/// with this struct and each front end converts it to its own brush type.
/// </remarks>
public readonly struct RgbaColor : IEquatable<RgbaColor>
{
    public RgbaColor(byte red, byte green, byte blue, byte alpha = 255)
    {
        R = red;
        G = green;
        B = blue;
        A = alpha;
    }

    public byte A { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    /// <summary>Parses <c>#RGB</c>, <c>#RRGGBB</c> or <c>#AARRGGBB</c>. Throws on garbage input.</summary>
    public static RgbaColor FromHex(string hex)
    {
        if (!TryFromHex(hex, out RgbaColor color))
        {
            throw new FormatException($"'{hex}' is not a valid color. Use #RGB, #RRGGBB or #AARRGGBB.");
        }

        return color;
    }

    /// <summary>Parses <c>#RGB</c>, <c>#RRGGBB</c> or <c>#AARRGGBB</c> without throwing.</summary>
    public static bool TryFromHex(string? hex, out RgbaColor color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        string value = hex.Trim();
        if (value.StartsWith('#'))
        {
            value = value.Substring(1);
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value.Substring(2);
        }

        if (value.Length != 3 && value.Length != 6 && value.Length != 8)
        {
            return false;
        }

        if (value.Length == 3)
        {
            value = string.Concat(value[0], value[0], value[1], value[1], value[2], value[2]);
        }

        if (value.Length == 8)
        {
            if (!TryParseByte(value.Substring(0, 2), out byte alpha))
            {
                return false;
            }

            if (!TryParseByte(value.Substring(2, 2), out byte r) ||
                !TryParseByte(value.Substring(4, 2), out byte g) ||
                !TryParseByte(value.Substring(6, 2), out byte b))
            {
                return false;
            }

            color = new RgbaColor(r, g, b, alpha);
            return true;
        }

        if (!TryParseByte(value.Substring(0, 2), out byte red) ||
            !TryParseByte(value.Substring(2, 2), out byte green) ||
            !TryParseByte(value.Substring(4, 2), out byte blue))
        {
            return false;
        }

        color = new RgbaColor(red, green, blue);
        return true;
    }

    /// <summary>Returns <c>#RRGGBB</c>, or <c>#AARRGGBB</c> when the color is translucent.</summary>
    public string ToHex() => A == 255
        ? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", R, G, B)
        : string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", A, R, G, B);

    /// <summary>Packed 0xAARRGGBB value, handy for Win32 and WinUI interop.</summary>
    public uint ToArgb() => ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;

    /// <summary>Same color with a different alpha.</summary>
    public RgbaColor WithAlpha(byte alpha) => new(R, G, B, alpha);

    /// <summary>Relative luminance in the 0..1 range (WCAG formula).</summary>
    public double Luminance()
    {
        static double Channel(byte value)
        {
            double c = value / 255d;
            return c <= 0.03928d ? c / 12.92d : Math.Pow((c + 0.055d) / 1.055d, 2.4d);
        }

        return (0.2126d * Channel(R)) + (0.7152d * Channel(G)) + (0.0722d * Channel(B));
    }

    /// <summary>Black or white, whichever is readable on top of this color.</summary>
    public RgbaColor ReadableForeground() => Luminance() > 0.42d ? new RgbaColor(11, 13, 17) : new RgbaColor(255, 255, 255);

    /// <inheritdoc />
    public bool Equals(RgbaColor other) => A == other.A && R == other.R && G == other.G && B == other.B;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RgbaColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(A, R, G, B);

    /// <inheritdoc />
    public override string ToString() => ToHex();

    public static bool operator ==(RgbaColor left, RgbaColor right) => left.Equals(right);

    public static bool operator !=(RgbaColor left, RgbaColor right) => !left.Equals(right);

    private static bool TryParseByte(string text, out byte value) =>
        byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
}
