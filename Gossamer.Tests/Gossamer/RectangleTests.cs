using System.Numerics;

namespace Gossamer.Tests.Gossamer;

[TestClass]
public class RectangleTests
{
    [TestMethod]
    public void TestWidth()
    {
        var rect = new Rectangle(0, 0, 10, 5);
        Assert.AreEqual(10, rect.Width);
    }

    [TestMethod]
    public void TestHeight()
    {
        var rect = new Rectangle(0, 0, 10, 5);
        Assert.AreEqual(5, rect.Height);
    }

    [TestMethod]
    public void TestPosition()
    {
        var rect = new Rectangle(1, 2, 10, 5);
        Assert.AreEqual(new Vector2(1, 2), rect.Position);
    }

    [TestMethod]
    public void TestSize()
    {
        var rect = new Rectangle(0, 0, 10, 5);
        Assert.AreEqual(new Vector2(10, 5), rect.Size);
    }

    [TestMethod]
    public void TestCenter()
    {
        var rect = new Rectangle(0, 0, 10, 10);
        Assert.AreEqual(new Vector2(5, 5), rect.Center);
    }

    [TestMethod]
    public void TestContains()
    {
        var rect = new Rectangle(0, 0, 10, 10);
        Assert.IsTrue(rect.Contains(new Vector2(5, 5)));
        Assert.IsFalse(rect.Contains(new Vector2(15, 5)));
    }

    [TestMethod]
    public void TestCenterOn()
    {
        var rect = new Rectangle(0, 0, 10, 10);
        var centeredRect = rect.CenterOn(new Vector2(20, 20));
        Assert.AreEqual(new Rectangle(15, 15, 25, 25), centeredRect);
    }

    [TestMethod]
    public void TestCenterOnOtherRect()
    {
        var rect1 = new Rectangle(0, 0, 10, 10);
        var rect2 = new Rectangle(0, 0, 20, 20);
        var centeredRect = rect1.CenterOn(rect2);
        Assert.AreEqual(new Rectangle(5, 5, 15, 15), centeredRect);
    }

    [TestMethod]
    public void TestCrop()
    {
        var rect = new Rectangle(0, 0, 10, 10);
        var croppedRect = rect.Crop(1, 1, 1, 1);
        Assert.AreEqual(new Rectangle(1, 1, 9, 9), croppedRect);
    }

    [TestMethod]
    public void TestScale()
    {
        var rect = new Rectangle(0, 0, 10, 10);
        var scaledRect = rect.Scale(2, 2);
        Assert.AreEqual(new Rectangle(0, 0, 20, 20), scaledRect);
    }

    [TestMethod]
    public void TestScaleWithVector()
    {
        var rect = new Rectangle(0, 0, 10, 10);
        var scaledRect = rect.Scale(new Vector2(2, 2));
        Assert.AreEqual(new Rectangle(0, 0, 20, 20), scaledRect);
    }

    [TestMethod]
    public void TestMove()
    {
        var rect = new Rectangle(0, 0, 10, 10);
        var movedRect = rect.Move(5, 5);
        Assert.AreEqual(new Rectangle(5, 5, 15, 15), movedRect);
    }

    [TestMethod]
    public void TestMoveWithVector()
    {
        var rect = new Rectangle(0, 0, 10, 10);
        var movedRect = rect.Move(new Vector2(5, 5));
        Assert.AreEqual(new Rectangle(5, 5, 15, 15), movedRect);
    }

    [TestMethod]
    public void TestClamp()
    {
        var rect1 = new Rectangle(0, 0, 10, 10);
        var rect2 = new Rectangle(5, 5, 15, 15);
        var clampedRect = rect1.Clamp(rect2);
        Assert.AreEqual(new Rectangle(5, 5, 10, 10), clampedRect);
    }

    [TestMethod]
    public void TestFromPositionSize()
    {
        var rect = Rectangle.FromPositionSize(new Vector2(1, 1), new Vector2(10, 10));
        Assert.AreEqual(new Rectangle(1, 1, 11, 11), rect);
    }

    [TestMethod]
    public void TestFromXYWH()
    {
        var rect = Rectangle.FromXYWH(1, 1, 10, 10);
        Assert.AreEqual(new Rectangle(1, 1, 11, 11), rect);
    }

    [TestMethod]
    public void TestFromLTRB()
    {
        var rect = Rectangle.FromLTRB(1, 1, 10, 10);
        Assert.AreEqual(new Rectangle(1, 1, 10, 10), rect);
    }
}