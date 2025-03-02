using System.Globalization;
using System.Runtime.InteropServices;

using Gossamer.Utilities;

namespace Gossamer;

[StructLayout(LayoutKind.Sequential)]
public struct Color(float r, float g, float b, float a) : IEquatable<Color>
{
    public float R = float.IsFinite(r) ? MathF.Max(0, r) : throw new ArgumentOutOfRangeException(nameof(r));
    public float G = float.IsFinite(g) ? MathF.Max(0, g) : throw new ArgumentOutOfRangeException(nameof(g));
    public float B = float.IsFinite(b) ? MathF.Max(0, b) : throw new ArgumentOutOfRangeException(nameof(b));
    public float A = float.IsFinite(a) ? MathUtilities.Clamp(a, 0, 1) : throw new ArgumentOutOfRangeException(nameof(a));

    /// <summary>
    /// Returns <see langword="true"/> if the color is transparent.
    /// </summary>
    public readonly bool IsTransparent => A == 0;

    public Color(Vector3 rgb) : this(rgb.X, rgb.Y, rgb.Z) { }
    public Color(Vector4 rgba) : this(rgba.X, rgba.Y, rgba.Z, rgba.W) { }
    public Color(float r, float g, float b) : this(r, g, b, 1) { }

    public readonly Color WithAlpha(float a) => new(R, G, B, a);
    public readonly Vector3 ToVector3() => new(R, G, B);
    public readonly Vector4 ToVector4() => new(R, G, B, A);

    /// <summary>
    /// Parses a color from a uint, e.g. 0x2B2A33 to R: 0.168627456, G: 0.164705887, B: 0.2, A: 1.
    /// </summary>
    /// <param name="rgb"></param>
    public static Color ParseUInt(uint rgb)
    {
        return new(
            ((rgb & 0xff0000) >> 16) / 255.0f,
            ((rgb & 0x00ff00) >> 08) / 255.0f,
            ((rgb & 0x0000ff) >> 00) / 255.0f,
            1.0f);
    }

    /// <summary>
    /// Parses a color from a hex string, e.g. #2B2A33 to R: 0.168627456, G: 0.164705887, B: 0.2, A: 1.
    /// <para> The hex string can be prefixed with a # or not. </para>
    /// </summary>
    /// <param name="hexString"></param>
    public static Color ParseHexString(ReadOnlySpan<char> hexString)
    {
        // Trim possible # from the start
        hexString = hexString.TrimStart('#');

        if (uint.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hexNumber))
        {
            return ParseUInt(hexNumber);
        }

        throw new FormatException("Invalid hex string.");
    }

    /// <summary>
    /// Converts the color to a uint, e.g. 0x2B2A33.
    /// </summary>
    public readonly uint ToUInt()
    {
        uint value = 0;
        value |= (uint)(R * 255.0f) << 16;
        value |= (uint)(G * 255.0f) << 8;
        value |= (uint)(B * 255.0f) << 0;
        return value;
    }

    /// <summary>
    /// Converts the color to a hex string, e.g. #2B2A33.
    /// </summary>
    public readonly string ToHexString()
    {
        return string.Format(CultureInfo.InvariantCulture, "#{0:X}", ToUInt());
    }

    /// <summary>
    /// Converts the color to a string, e.g. R: 0.168627456, G: 0.164705887, B: 0.2, A: 1.
    /// </summary>
    public override readonly string ToString()
    {
        return $"R: {R}, G: {G}, B: {B}, A: {A}";
    }

    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    /// <param name="color1"></param>
    /// <param name="color2"></param>
    /// <param name="amount"></param>
    public static Color Lerp(Color color1, Color color2, float amount)
    {
        return new Color(Vector4.Lerp(color1.ToVector4(), color2.ToVector4(), amount));
    }

    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !(left == right);
    public readonly bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;
    public override readonly bool Equals(object? obj) => obj is Color color && Equals(color);
    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);

    public static readonly Color White = new(1, 1, 1, 1);
    public static readonly Color Black = new(0, 0, 0, 1);
    public static readonly Color Transparent = new(0, 0, 0, 0);

    public static readonly Color HighlighterRed = ParseHexString("#ef5777");
    public static readonly Color SizzlingRed = ParseHexString("#f53b57");

    public static readonly Color DarkPeriwinkle = ParseHexString("#575fcf");
    public static readonly Color FreeSpeechBlue = ParseHexString("#3c40c6");

    public static readonly Color Megaman = ParseHexString("#4bcffa");
    public static readonly Color SpiroDiscoBall = ParseHexString("#0fbcf9");

    public static readonly Color FreshTurquoise = ParseHexString("#34e7e4");
    public static readonly Color JadeDust = ParseHexString("#00d8d6");

    public static readonly Color MintyGreen = ParseHexString("#0be881");
    public static readonly Color GreenTeal = ParseHexString("#05c46b");

    public static readonly Color SunsetOrange = ParseHexString("#ff5e57");
    public static readonly Color RedOrange = ParseHexString("#ff3f34");

    public static readonly Color ElusiveBlue = ParseHexString("#d2dae2");
    public static readonly Color LondonSquare = ParseHexString("#808e9b");

    public static readonly Color GoodNight = ParseHexString("#485460");
    public static readonly Color BlackPearl = ParseHexString("#1e272e");

    public static readonly Color YrielYellow = ParseHexString("#ffdd59");
    public static readonly Color VibrantYellow = ParseHexString("#ffd32a");
}