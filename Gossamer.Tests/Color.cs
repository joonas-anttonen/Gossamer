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
}
