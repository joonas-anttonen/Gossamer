using System.Numerics;

using Gossamer;

namespace Gossamer.Tests;

[TestClass]
public sealed class ColorTests
{
    [TestMethod]
    public void OutOfRange()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Color(float.NaN, 0, 0, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Color(0, float.NaN, 0, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Color(0, 0, float.NaN, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Color(0, 0, 0, float.NaN));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Color(float.PositiveInfinity, 0, 0, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Color(0, float.PositiveInfinity, 0, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Color(0, 0, float.PositiveInfinity, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Color(0, 0, 0, float.PositiveInfinity));
    }

    [TestMethod]
    public void Clamping()
    {
        Color color = new(-1, -1, -1);
        Assert.AreEqual(0, color.R);
        Assert.AreEqual(0, color.G);
        Assert.AreEqual(0, color.B);

        color = Color.White.WithAlpha(float.MaxValue);
        Assert.AreEqual(1.0f, color.A);

        color = Color.White.WithAlpha(float.MinValue);
        Assert.AreEqual(0.0f, color.A);
    }

    [TestMethod]
    public void Parsing()
    {
        Color color = Color.ParseUInt(0x2B2A33);
        Assert.AreEqual(0.168627456f, color.R);
        Assert.AreEqual(0.164705887f, color.G);
        Assert.AreEqual(0.2f, color.B);
        Assert.AreEqual(1.0f, color.A);

        color = Color.ParseHexString("#ef5777");
        Assert.AreEqual(0.9372549f, color.R);
        Assert.AreEqual(0.34117648f, color.G);
        Assert.AreEqual(0.46666667f, color.B);
        Assert.AreEqual(1.0f, color.A);

        Assert.ThrowsException<FormatException>(() => Color.ParseHexString("invalid"));
    }

    [TestMethod]
    public void Stringify()
    {
        Color color = Color.ParseUInt(0x2B2A33);
        Assert.AreEqual("#2B2A33", color.ToHexString());

        color = Color.ParseHexString("#ef5777");
        Assert.AreEqual("#EF5777", color.ToHexString());
    }

    [TestMethod]
    public void Equality()
    {
        Color color1 = Color.ParseUInt(0x2B2A33);
        Color color2 = Color.ParseUInt(0x2B2A33);

        Assert.IsTrue(color1 == color2);
        Assert.IsFalse(color1 != color2);
        Assert.IsTrue(color1.Equals(color2));
        Assert.IsTrue(color1.Equals((object)color2));
        Assert.IsFalse(color1.Equals(null));
        Assert.AreEqual(color1.GetHashCode(), color2.GetHashCode());

        Color color3 = Color.ParseUInt(0x2B2A34);

        Assert.IsFalse(color1 == color3);
        Assert.IsTrue(color1 != color3);
        Assert.IsFalse(color1.Equals(color3));
        Assert.IsFalse(color1.Equals((object)color3));
        Assert.IsFalse(color1.Equals(null));
        Assert.AreNotEqual(color1.GetHashCode(), color3.GetHashCode());
    }

    [TestMethod]
    public void Math()
    {
        Color color1 = Color.ParseUInt(0x2B2A33);
        Color color2 = Color.ParseUInt(0x2B2A34);

        Color lerp = Color.Lerp(color1, color2, 0.5f);
        Assert.AreEqual((0.168627456f + 0.168627456f) / 2, lerp.R);
        Assert.AreEqual((0.164705887f + 0.164705887f) / 2, lerp.G);
        Assert.AreEqual((0.2f + 0.2f) / 2, lerp.B, 0.1f);
        Assert.AreEqual(1.0f, lerp.A);

        lerp = Color.Lerp(color1, color2, 0.0f);
        Assert.AreEqual(0.168627456f, lerp.R);
        Assert.AreEqual(0.164705887f, lerp.G);
        Assert.AreEqual(0.2f, lerp.B);
        Assert.AreEqual(1.0f, lerp.A);
    }

    [TestMethod]
    public void VectorConversion()
    {
        Color color = Color.ParseUInt(0x2B2A33);
        Vector4 vector4 = color.ToVector4();
        Assert.AreEqual(color.R, vector4.X);
        Assert.AreEqual(color.G, vector4.Y);
        Assert.AreEqual(color.B, vector4.Z);
        Assert.AreEqual(color.A, vector4.W);

        vector4 = new Vector4(0.168627456f, 0.164705887f, 0.2f, 1.0f);
        color = new Color(vector4);
        Assert.AreEqual(vector4.X, color.R);
        Assert.AreEqual(vector4.Y, color.G);
        Assert.AreEqual(vector4.Z, color.B);
        Assert.AreEqual(vector4.W, color.A);

        Vector3 vector3 = color.ToVector3();
        Assert.AreEqual(color.R, vector3.X);
        Assert.AreEqual(color.G, vector3.Y);
        Assert.AreEqual(color.B, vector3.Z);

        vector3 = new Vector3(0.168627456f, 0.164705887f, 0.2f);
        color = new Color(vector3);
        Assert.AreEqual(vector3.X, color.R);
        Assert.AreEqual(vector3.Y, color.G);
        Assert.AreEqual(vector3.Z, color.B);
        Assert.AreEqual(1.0f, color.A);
    }

    [TestMethod]
    public void ColorPalettes()
    {
        Assert.AreEqual("#2E3440", Color.Palettes.Nord.Nord0_Darkest.ToHexString());
        Assert.AreEqual("#3B4252", Color.Palettes.Nord.Nord1_Darker.ToHexString());
        Assert.AreEqual("#434C5E", Color.Palettes.Nord.Nord2.ToHexString());
        Assert.AreEqual("#4C566A", Color.Palettes.Nord.Nord3.ToHexString());
        Assert.AreEqual("#D8DEE9", Color.Palettes.Nord.Nord4_White.ToHexString());
        Assert.AreEqual("#E5E9F0", Color.Palettes.Nord.Nord5_White.ToHexString());
        Assert.AreEqual("#ECEFF4", Color.Palettes.Nord.Nord6_White.ToHexString());
        Assert.AreEqual("#8FBCBB", Color.Palettes.Nord.Nord7_GreenishBlue.ToHexString());
        Assert.AreEqual("#88C0D0", Color.Palettes.Nord.Nord8_LightestBlue.ToHexString());
        Assert.AreEqual("#81A1C1", Color.Palettes.Nord.Nord9_LightBlue.ToHexString());
        Assert.AreEqual("#5E81AC", Color.Palettes.Nord.Nord10_DarkBlue.ToHexString());
        Assert.AreEqual("#BF616A", Color.Palettes.Nord.Nord11_Red.ToHexString());
        Assert.AreEqual("#D08770", Color.Palettes.Nord.Nord12_Orange.ToHexString());
        Assert.AreEqual("#EBCB8B", Color.Palettes.Nord.Nord13_Yellow.ToHexString());
        Assert.AreEqual("#A3BE8C", Color.Palettes.Nord.Nord14_Green.ToHexString());
        Assert.AreEqual("#B48EAD", Color.Palettes.Nord.Nord15_Purple.ToHexString());
    }

    [TestMethod]
    public void ForCompleteness()
    {
        // IsTransparent
        Assert.IsTrue(Color.Transparent.IsTransparent);
        Assert.IsFalse(Color.White.IsTransparent);
    }
}
