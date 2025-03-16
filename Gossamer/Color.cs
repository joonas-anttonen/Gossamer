using System.Diagnostics.CodeAnalysis;
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

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    public static class Palettes
    {
        /// <summary>
        /// The Nord color palette. 
        /// <para> Nord0-Nord3 are dark, Nord4-Nord6 are light, Nord7-Nord10 are blue/greenish, Nord11-Nord15 are red/yellow/green/purple. </para>
        /// </summary>
        public static class Nord
        {
            /// <summary> #2e3440 </summary>
            public static readonly Color Nord0_Darkest = new(0.18039216f, 0.20392157f, 0.2509804f);
            /// <summary> #3b4252 </summary>
            public static readonly Color Nord1_Darker = new(0.23137255f, 0.25882354f, 0.32156864f);
            /// <summary> #434c5e </summary>
            public static readonly Color Nord2 = new(0.2627451f, 0.29803923f, 0.36862746f);
            /// <summary> #4c566a </summary>
            public static readonly Color Nord3 = new(0.29803923f, 0.3372549f, 0.41568628f);

            /// <summary> #d8dee9 </summary>
            public static readonly Color Nord4_White = new(0.84705883f, 0.87058824f, 0.9137255f);
            /// <summary> #e5e9f0 </summary>
            public static readonly Color Nord5_White = new(0.8980392f, 0.9137255f, 0.9411765f);
            /// <summary> #eceff4 </summary>
            public static readonly Color Nord6_White = new(0.9254902f, 0.9372549f, 0.95686275f);

            /// <summary> #8fbcbb </summary>
            public static readonly Color Nord7_GreenishBlue = new(0.56078434f, 0.7372549f, 0.73333335f);
            /// <summary> #88c0d0 </summary>
            public static readonly Color Nord8_LightestBlue = new(0.53333336f, 0.7529412f, 0.8156863f);
            /// <summary> #81a1c1 </summary>
            public static readonly Color Nord9_LightBlue = new(0.5058824f, 0.6313726f, 0.75686276f);
            /// <summary> #5e81ac </summary>
            public static readonly Color Nord10_DarkBlue = new(0.36862746f, 0.5058824f, 0.6745098f);

            /// <summary> #bf616a </summary>
            public static readonly Color Nord11_Red = new(0.7490196f, 0.38039216f, 0.41568628f);
            /// <summary> #d08770 </summary>
            public static readonly Color Nord12_Orange = new(0.8156863f, 0.5294118f, 0.4392157f);
            /// <summary> #ebcb8b </summary>
            public static readonly Color Nord13_Yellow = new(0.92156863f, 0.79607844f, 0.54509807f);
            /// <summary> #a3be8c </summary>
            public static readonly Color Nord14_Green = new(0.6392157f, 0.74509805f, 0.54901963f);
            /// <summary> #b48ead </summary>
            public static readonly Color Nord15_Purple = new(0.7058824f, 0.5568628f, 0.6784314f);
        }

        public static class Swedish
        {
            public static readonly Color HighlighterRed = new(0.9372549f, 0.34117648f, 0.46666667f);
            public static readonly Color SizzlingRed = new(0.9607843f, 0.23137255f, 0.34117648f);

            public static readonly Color DarkPeriwinkle = new(0.34117648f, 0.37254903f, 0.8117647f);
            public static readonly Color FreeSpeechBlue = new(0.23529412f, 0.2509804f, 0.7764706f);

            public static readonly Color Megaman = new(0.29411766f, 0.8117647f, 0.98039216f);
            public static readonly Color SpiroDiscoBall = new(0.05882353f, 0.7372549f, 0.9764706f);

            public static readonly Color FreshTurquoise = new(0.20392157f, 0.90588236f, 0.89411765f);
            public static readonly Color JadeDust = new(0.0f, 0.84705883f, 0.8392157f);

            public static readonly Color MintyGreen = new(0.043137256f, 0.9098039f, 0.5058824f);
            public static readonly Color GreenTeal = new(0.019607844f, 0.76862746f, 0.41960785f);

            public static readonly Color SunsetOrange = new(1.0f, 0.36862746f, 0.34117648f);
            public static readonly Color RedOrange = new(1.0f, 0.24705882f, 0.20392157f);

            public static readonly Color ElusiveBlue = new(0.8235294f, 0.85490197f, 0.8862745f);
            public static readonly Color LondonSquare = new(0.5019608f, 0.5568628f, 0.60784316f);

            public static readonly Color GoodNight = new(0.28235295f, 0.32941177f, 0.3764706f);
            public static readonly Color BlackPearl = new(0.11764706f, 0.15294118f, 0.18039216f);

            public static readonly Color YrielYellow = new(1.0f, 0.8666667f, 0.34901962f);
            public static readonly Color VibrantYellow = new(1.0f, 0.827451f, 0.16470589f);
        }
    }
}