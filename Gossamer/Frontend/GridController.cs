namespace Gossamer.Frontend;

public class GridController : IGridControllable
{
    private readonly IGridControllable[] descendants;
    private readonly GridDefinition gridDefinition;

    public record struct Cell(IGridControllable Controllable, Rectangle Layout);

    public GridPlacement GridPlacement { get; set; } = new GridPlacement(0, 0, 1, 1);

    public Cell[] CellLayouts { get; private set; }

    public GridController(GridDefinition gridDefinition, IGridControllable[] descendants)
    {
        this.descendants = descendants;
        this.gridDefinition = gridDefinition;

        CellLayouts = new Cell[gridDefinition.Columns.Length * gridDefinition.Rows.Length];

        for (int i = 0; i < descendants.Length; i++)
        {
            IGridControllable descendant = descendants[i];

            GridPlacement placement = descendant.GridPlacement;




        }
    }

    public Vector2 Measure(Vector2 availableSize)
    {
        return availableSize;
    }

    public void Arrange(Rectangle layoutRectangle)
    {
        for (int i = 0; i < descendants.Length; i++)
        {
            IGridControllable descendant = descendants[i];

            //descendant.Arrange(CellLayouts[i]);
        }
    }
}