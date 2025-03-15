using System.Numerics;

using Gossamer.Frontend;

namespace Gossamer.Tests.Gossamer.Frontend;

/*[TestClass]
public class GridControllerTests
{
    [TestMethod]
    public void Basic()
    {
        GridControllable controllable = new()
        {
            GridPlacement = new(0, 0, 1, 1),
            HorizontalAlignment = Alignment.Auto,
            VerticalAlignment = Alignment.Auto,
        };

        GridDefinition definition = new(
            [
                Measure.Fr(1),
            ],
            [
                Measure.Fr(1),
            ]);
        GridController grid = new(definition, [controllable])
        {
            //HorizontalAlignment = Alignment.Auto,
            //VerticalAlignment = Alignment.Auto,
        };

        Vector2 wantedSize = grid.Measure(new Vector2(100, 100));

        Assert.AreEqual(new Vector2(100, 100), wantedSize);

        grid.Arrange(new Rectangle(0, 0, 100, 100));

        Assert.AreEqual(new Rectangle(0, 0, 100, 100), controllable.LayoutRectangle);
    }
}*/

public class GridControllable : IGridControllable
{
    public GridPlacement GridPlacement { get; init; }

    public Alignment HorizontalAlignment { get; init; }

    public Alignment VerticalAlignment { get; init; }

    public Rectangle LayoutRectangle { get; private set; }

    public void Arrange(Rectangle layoutRectangle)
    {
        LayoutRectangle = layoutRectangle;
    }

    public Vector2 Measure(Vector2 spaceAvailable)
    {
        return spaceAvailable;
    }
}