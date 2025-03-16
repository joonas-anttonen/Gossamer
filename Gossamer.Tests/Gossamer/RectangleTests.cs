namespace Gossamer.Tests.Gossamer;

[TestClass]
public class RectangleTests
{
    [TestMethod]
    public void Rectangle_From()
    {
        Rectangle rect = Rectangle.FromLTRB(1, 1, 10, 10);
        Assert.AreEqual(new Rectangle(1, 1, 10, 10), rect);

        rect = Rectangle.FromXYWH(1, 1, 10, 10);
        Assert.AreEqual(new Rectangle(1, 1, 11, 11), rect);

        rect = Rectangle.FromPositionSize(new Vector2(1, 1), new Vector2(10, 10));
        Assert.AreEqual(new Rectangle(1, 1, 11, 11), rect);
    }
    
    [TestMethod]
    public void Rectangle_Properties()
    {
        Rectangle rect = new(0, 0, 10, 5);
        Assert.AreEqual(10, rect.Width);

        rect = new(0, 0, 10, 5);
        Assert.AreEqual(5, rect.Height);

        rect = new(1, 2, 10, 5);
        Assert.AreEqual(new Vector2(1, 2), rect.Position);

        rect = new(0, 0, 10, 5);
        Assert.AreEqual(new Vector2(10, 5), rect.Size);

        rect = new(0, 0, 10, 10);
        Assert.AreEqual(new Vector2(5, 5), rect.Center);
    }

    [TestMethod]
    public void Rectangle_Contains()
    {
        Rectangle rect = new(0, 0, 10, 10);
        Assert.IsTrue(rect.Contains(new Vector2(5, 5)));
        Assert.IsFalse(rect.Contains(new Vector2(15, 5)));
    }

    [TestMethod]
    public void Rectangle_CenterOn()
    {
        Rectangle rect = new(0, 0, 10, 10);
        Rectangle centeredRect = rect.CenterOn(new Vector2(20, 20));
        Assert.AreEqual(new Rectangle(15, 15, 25, 25), centeredRect);

        Rectangle rect1 = new(0, 0, 10, 10);
        Rectangle rect2 = new(0, 0, 20, 20);
        centeredRect = rect1.CenterOn(rect2);
        Assert.AreEqual(new Rectangle(5, 5, 15, 15), centeredRect);
    }

    [TestMethod]
    public void Rectangle_Crop()
    {
        Rectangle rect = new(0, 0, 10, 10);
        Rectangle croppedRect = rect.Crop(1, 1, 1, 1);
        Assert.AreEqual(new Rectangle(1, 1, 9, 9), croppedRect);
    }

    [TestMethod]
    public void Rectangle_Scale()
    {
        Rectangle rect = new(0, 0, 10, 10);
        Rectangle scaledRect = rect.Scale(2, 2);
        Assert.AreEqual(new Rectangle(0, 0, 20, 20), scaledRect);

        rect = new Rectangle(0, 0, 10, 10);
        scaledRect = rect.Scale(new Vector2(2, 2));
        Assert.AreEqual(new Rectangle(0, 0, 20, 20), scaledRect);
    }

    [TestMethod]
    public void Rectangle_Move()
    {
        Rectangle rect = new(0, 0, 10, 10);
        Rectangle movedRect = rect.Move(5, 5);
        Assert.AreEqual(new Rectangle(5, 5, 15, 15), movedRect);

        rect = new Rectangle(0, 0, 10, 10);
        movedRect = rect.Move(new Vector2(5, 5));
        Assert.AreEqual(new Rectangle(5, 5, 15, 15), movedRect);
    }

    [TestMethod]
    public void Rectangle_Clamp()
    {
        Rectangle rect1 = new(0, 0, 10, 10);
        Rectangle rect2 = new(5, 5, 15, 15);
        Rectangle clampedRect = rect1.Clamp(rect2);
        Assert.AreEqual(new Rectangle(5, 5, 10, 10), clampedRect);
    }
}