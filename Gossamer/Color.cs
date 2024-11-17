using System.Runtime.InteropServices;

namespace Gossamer;

[StructLayout(LayoutKind.Sequential)]
public struct Color : IEquatable<Color>
{
    public float R;
    public float G;
    public float B;
    public float A;

    public readonly Vector3 ToVector3() => new(R, G, B);
    public readonly Vector4 ToVector4() => new(R, G, B, A);

    public Color(Vector3 rgb) : this(rgb.X, rgb.Y, rgb.Z) { }
    public Color(Vector4 rgba) : this(rgba.X, rgba.Y, rgba.Z, rgba.W) { }
    public Color(Color rgb, float a) : this(rgb.R, rgb.G, rgb.B, a) { }
    public Color(float r, float g, float b) : this(r, g, b, 1) { }
    public Color(float r, float g, float b, float a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    Color(uint rgba)
    {
        R = ((rgba & 0xff000000) >> 24) / 255.0f;
        G = ((rgba & 0x00ff0000) >> 16) / 255.0f;
        B = ((rgba & 0x0000ff00) >> 08) / 255.0f;
        A = ((rgba & 0x000000ff) >> 00) / 255.0f;
    }

    // Gamma ramps and encoding transfer functions
    //
    // Orthogonal to color space though usually tightly coupled.  For instance, sRGB is both a
    // color space (defined by three basis vectors and a white point) and a gamma ramp.  Gamma
    // ramps are designed to reduce perceptual error when quantizing floats to integers with a
    // limited number of bits.  More variation is needed in darker colors because our eyes are
    // more sensitive in the dark.  The way the curve helps is that it spreads out dark values
    // across more code words allowing for more variation.  Likewise, bright values are merged
    // together into fewer code words allowing for less variation.
    //
    // The sRGB curve is not a true gamma ramp but rather a piecewise function comprising a linear
    // section and a power function.  When sRGB-encoded colors are passed to an LCD monitor, they
    // look correct on screen because the monitor expects the colors to be encoded with sRGB, and it
    // removes the sRGB curve to linearize the values.  When textures are encoded with sRGB--as many
    // are--the sRGB curve needs to be removed before involving the colors in linear mathematics such
    // as physically based lighting.

    public static Color ApplySRGBCurve(Color x) => new(ApplySRGBCurve(x.R), ApplySRGBCurve(x.G), ApplySRGBCurve(x.B), x.A);
    public static Color RemoveSRGBCurve(Color x) => new(RemoveSRGBCurve(x.R), RemoveSRGBCurve(x.G), RemoveSRGBCurve(x.B), x.A);

    static float ApplySRGBCurve(float x) => x < 0.0031308f ? 12.92f * x : 1.055f * MathF.Pow(x, 1.0f / 2.4f) - 0.055f;
    static float RemoveSRGBCurve(float x) => x < 0.04045f ? x / 12.92f : MathF.Pow((x + 0.055f) / 1.055f, 2.4f);

    public static Color UnpackRGBA(uint rgba) => new(rgba);
    public static Color UnpackRGB(uint rgb) => new(rgb << 8 | 0xff);
    public static Color UnpackRGB(ReadOnlySpan<char> hexString)
    {
        // Trim possible # from the start
        hexString = hexString.TrimStart('#');

        if (uint.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out uint hexNumber))
        {
            if (hexString.Length == 8)
                return UnpackRGBA(hexNumber);
            else
                return UnpackRGB(hexNumber);
        }
        else
        {
            return SizzlingRed;
        }
    }

    public static uint PackRGBA(Color color)
    {
        uint value = 0;
        value |= (uint)(color.R * 255.0f) << 24;
        value |= (uint)(color.G * 255.0f) << 16;
        value |= (uint)(color.B * 255.0f) << 8;
        value |= (uint)(color.A * 255.0f) << 0;
        return value;
    }

    static uint PackRGB(Color color)
    {
        uint value = 0;
        value |= (uint)(color.R * 255.0f) << 16;
        value |= (uint)(color.G * 255.0f) << 8;
        value |= (uint)(color.B * 255.0f) << 0;
        return value;
    }

    public static string ToHexString(Color color)
        => string.Format(System.Globalization.CultureInfo.InvariantCulture, "#{0:X}", PackRGB(color));

    public static Color Lerp(Color color1, Color color2, float amount)
    {
        return new Color(Vector4.Lerp(color1.ToVector4(), color2.ToVector4(), amount));
    }

    public static Color operator *(float f, Color right) => new(right.R * f, right.G * f, right.B * f, right.A * f);
    public static Color operator *(Color left, float f) => new(left.R * f, left.G * f, left.B * f, left.A * f);

    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !(left == right);
    public readonly bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;
    public override readonly bool Equals(object? obj) => obj is Color color && Equals(color);
    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);

    public static readonly Color White = new(1, 1, 1, 1);
    public static readonly Color Black = new(0, 0, 0, 1);
    public static readonly Color Transparent = new(0, 0, 0, 0);

    public static readonly Color HighlighterRed = UnpackRGB("#ef5777");
    public static readonly Color SizzlingRed = UnpackRGB("#f53b57");

    public static readonly Color DarkPeriwinkle = UnpackRGB("#575fcf");
    public static readonly Color FreeSpeechBlue = UnpackRGB("#3c40c6");

    public static readonly Color Megaman = UnpackRGB("#4bcffa");
    public static readonly Color SpiroDiscoBall = UnpackRGB("#0fbcf9");

    public static readonly Color FreshTurquoise = UnpackRGB("#34e7e4");
    public static readonly Color JadeDust = UnpackRGB("#00d8d6");

    public static readonly Color MintyGreen = UnpackRGB("#0be881");
    public static readonly Color GreenTeal = UnpackRGB("#05c46b");

    public static readonly Color SunsetOrange = UnpackRGB("#ff5e57");
    public static readonly Color RedOrange = UnpackRGB("#ff3f34");

    public static readonly Color ElusiveBlue = UnpackRGB("#d2dae2");
    public static readonly Color LondonSquare = UnpackRGB("#808e9b");

    public static readonly Color GoodNight = UnpackRGB("#485460");
    public static readonly Color BlackPearl = UnpackRGB("#1e272e");

    public static readonly Color YrielYellow = UnpackRGB("#ffdd59");
    public static readonly Color VibrantYellow = UnpackRGB("#ffd32a");
}