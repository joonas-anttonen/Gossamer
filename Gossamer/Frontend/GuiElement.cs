using Gossamer.Backend;
using Gossamer.Utilities;

namespace Gossamer.Frontend;

/// <summary>
/// Represents the units of measurement for GUI elements.
/// </summary>
public enum Unit
{
    /// <summary>
    /// Size is automatically determined.
    /// </summary>
    Auto,

    /// <summary>
    /// Size in pixels.
    /// </summary>
    Px,

    /// <summary>
    /// Size in em units, relative to the font size.
    /// </summary>
    Em,

    /// <summary>
    /// Size in fractional units, usually relative to the current container.
    /// </summary>
    Fr
}

public readonly record struct Measure(Unit Unit, float Value)
{
    public static Measure Px(float value) => new(Unit.Px, value);
    public static Measure Em(float value) => new(Unit.Em, value);
    public static Measure Fr(float value) => new(Unit.Fr, value);
}

public enum Visibility { Visible, Hidden, Collapsed }

public enum Alignment { Auto, Stretch, Center, Near, Far }

public record struct Outline(bool IsVisible, Color Color, Measure Width, Measure Offset)
{
    public static Outline None => new(
        false,
        Color.Transparent,
        Measure.Px(0),
        Measure.Px(0));
}

public record struct Spacing(Measure Left, Measure Top, Measure Right, Measure Bottom)
{
    public static Spacing None => new(
        Measure.Px(0),
        Measure.Px(0),
        Measure.Px(0),
        Measure.Px(0));

    public static Spacing Create(Measure all) => new(all, all, all, all);
    public static Spacing Create(Measure topbottom, Measure leftright) => new(leftright, topbottom, leftright, topbottom);
    public static Spacing Create(Measure top, Measure leftright, Measure bottom) => new(leftright, top, leftright, bottom);
    public static Spacing Create(Measure left, Measure top, Measure right, Measure bottom) => new(left, top, right, bottom);
}

[Flags]
public enum BorderVisibility { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8, All = Left | Top | Right | Bottom }
public record struct Border(BorderVisibility Visibility, Color Color, Spacing Spacing)
{
    public static Border None => new(
        BorderVisibility.None,
        Color.Transparent,
        Spacing.None);
}

class StyleComputer
{
    public static void Apply(Style style, PartialStyle partialStyle)
    {
        style.Visibility = partialStyle.Visibility ?? style.Visibility;

        style.FontSize = partialStyle.FontSize ?? style.FontSize;

        style.Background = partialStyle.Background ?? style.Background;
        style.Foreground = partialStyle.Foreground ?? style.Foreground;

        style.Outline = new(
            partialStyle.Outline_Style ?? style.Outline.IsVisible,
            partialStyle.Outline_Color ?? style.Outline.Color,
            partialStyle.Outline_Width ?? style.Outline.Width,
            partialStyle.Outline_Offset ?? style.Outline.Offset);

        BorderVisibility borderVisibility = style.Border.Visibility;
        borderVisibility |= partialStyle.Border_Left_Style.HasValue ? (partialStyle.Border_Left_Style.Value ? BorderVisibility.Left : BorderVisibility.None) : BorderVisibility.None;
        borderVisibility |= partialStyle.Border_Top_Style.HasValue ? (partialStyle.Border_Top_Style.Value ? BorderVisibility.Top : BorderVisibility.None) : BorderVisibility.None;
        borderVisibility |= partialStyle.Border_Right_Style.HasValue ? (partialStyle.Border_Right_Style.Value ? BorderVisibility.Right : BorderVisibility.None) : BorderVisibility.None;
        borderVisibility |= partialStyle.Border_Bottom_Style.HasValue ? (partialStyle.Border_Bottom_Style.Value ? BorderVisibility.Bottom : BorderVisibility.None) : BorderVisibility.None;
        style.Border = new(
            borderVisibility,
            partialStyle.Border_Color ?? style.Border.Color,
            new(
                partialStyle.Border_Left_Width ?? style.Border.Spacing.Left,
                partialStyle.Border_Top_Width ?? style.Border.Spacing.Top,
                partialStyle.Border_Right_Width ?? style.Border.Spacing.Right,
                partialStyle.Border_Bottom_Width ?? style.Border.Spacing.Bottom));

        style.Padding = new(
            partialStyle.Padding_Left ?? style.Padding.Left,
            partialStyle.Padding_Top ?? style.Padding.Top,
            partialStyle.Padding_Right ?? style.Padding.Right,
            partialStyle.Padding_Bottom ?? style.Padding.Bottom);

        style.Margin = new(
            partialStyle.Margin_Left ?? style.Margin.Left,
            partialStyle.Margin_Top ?? style.Margin.Top,
            partialStyle.Margin_Right ?? style.Margin.Right,
            partialStyle.Margin_Bottom ?? style.Margin.Bottom);
    }
}

/// <summary>
/// Represents a partial style that can be applied to a GUI element to modify some of its stylistic properties.
/// </summary>
public record PartialStyle
{
    public Visibility? Visibility { get; init; }

    public float? FontSize { get; init; }

    /// CSS background-color
    /// </summary>
    public Color? Background { get; init; }
    /// <summary>
    /// CSS color
    /// </summary>
    public Color? Foreground { get; init; }

    /// <summary>
    /// Pseudo-CSS outline-style. Regular CSS has all kinds of styles, we only have visible or not.
    /// </summary>
    public bool? Outline_Style { get; init; }
    /// <summary>
    /// CSS outline-width
    /// </summary>
    public Measure? Outline_Width { get; init; }
    /// <summary>
    /// CSS outline-offset
    /// </summary>
    public Measure? Outline_Offset { get; init; }
    /// <summary>
    /// CSS outline-color
    /// </summary>
    public Color? Outline_Color { get; init; }

    /// <summary>
    /// CSS margin-left
    /// </summary>
    public Measure? Margin_Left { get; init; }
    /// <summary>
    /// CSS margin-top
    /// </summary>
    public Measure? Margin_Top { get; init; }
    /// <summary>
    /// CSS margin-right
    /// </summary>
    public Measure? Margin_Right { get; init; }
    /// <summary>
    /// CSS margin-bottom
    /// </summary>
    public Measure? Margin_Bottom { get; init; }

    /// <summary>
    /// Pseudo-CSS border-color. Regular CSS is invididual color for each side.
    /// </summary>
    public Color? Border_Color { get; init; }
    /// <summary>
    /// CSS border-left-width
    /// </summary>
    public Measure? Border_Left_Width { get; init; }
    /// <summary>
    /// CSS border-top-width
    /// </summary>
    public Measure? Border_Top_Width { get; init; }
    /// <summary>
    /// CSS border-right-width
    /// </summary>
    public Measure? Border_Right_Width { get; init; }
    /// <summary>
    /// CSS border-bottom-width
    /// </summary>
    public Measure? Border_Bottom_Width { get; init; }

    /// <summary>
    /// Pseudo-CSS border-style-left. Regular CSS has all kinds of styles, we only have visible or not.
    /// </summary>
    public bool? Border_Left_Style { get; init; }
    /// <summary>
    /// Pseudo-CSS border-style-top. Regular CSS has all kinds of styles, we only have visible or not.
    /// </summary>
    public bool? Border_Top_Style { get; init; }
    /// <summary>
    /// Pseudo-CSS border-style-right. Regular CSS has all kinds of styles, we only have visible or not.
    /// </summary>
    public bool? Border_Right_Style { get; init; }
    /// <summary>
    /// Pseudo-CSS border-style-bottom. Regular CSS has all kinds of styles, we only have visible or not.
    /// </summary>
    public bool? Border_Bottom_Style { get; init; }

    /// <summary>
    /// CSS padding-left
    /// </summary>
    public Measure? Padding_Left { get; init; }
    /// <summary>
    /// CSS padding-top
    /// </summary>
    public Measure? Padding_Top { get; init; }
    /// <summary>
    /// CSS padding-right
    /// </summary>
    public Measure? Padding_Right { get; init; }
    /// <summary>
    /// CSS padding-bottom
    /// </summary>
    public Measure? Padding_Bottom { get; init; }

    public static PartialStyle FromStyle(IReadOnlyStyle style)
    {
        return new()
        {
            FontSize = style.FontSize,
            Background = style.Background,
            Foreground = style.Foreground,
            Outline_Style = style.Outline.IsVisible,
            Outline_Width = style.Outline.Width,
            Outline_Offset = style.Outline.Offset,
            Outline_Color = style.Outline.Color,
            Margin_Left = style.Margin.Left,
            Margin_Top = style.Margin.Top,
            Margin_Right = style.Margin.Right,
            Margin_Bottom = style.Margin.Bottom,
            Border_Color = style.Border.Color,
            Border_Left_Width = style.Border.Spacing.Left,
            Border_Top_Width = style.Border.Spacing.Top,
            Border_Right_Width = style.Border.Spacing.Right,
            Border_Bottom_Width = style.Border.Spacing.Bottom,
            Border_Left_Style = style.Border.Visibility.HasFlag(BorderVisibility.Left),
            Border_Top_Style = style.Border.Visibility.HasFlag(BorderVisibility.Top),
            Border_Right_Style = style.Border.Visibility.HasFlag(BorderVisibility.Right),
            Border_Bottom_Style = style.Border.Visibility.HasFlag(BorderVisibility.Bottom),
            Padding_Left = style.Padding.Left,
            Padding_Top = style.Padding.Top,
            Padding_Right = style.Padding.Right,
            Padding_Bottom = style.Padding.Bottom
        };
    }
}

public record GridDefinition(Measure[] Columns, Measure[] Rows);

public record struct GridPlacement(int Column, int Row, int ColumnSpan, int RowSpan);

public interface IReadOnlyStyle
{
    Visibility Visibility { get; }
    float? FontSize { get; }
    Color Background { get; }
    Color Foreground { get; }
    Outline Outline { get; }
    Border Border { get; }
    Spacing Padding { get; }
    Spacing Margin { get; }
    Alignment HorizontalAlignment { get; }
    Alignment VerticalAlignment { get; }
    Measure? Width { get; }
    Measure? Height { get; }
    Measure? MinWidth { get; }
    Measure? MinHeight { get; }
    Measure? MaxWidth { get; }
    Measure? MaxHeight { get; }
}

/// <summary>
/// Represents the style of a GUI element.
/// </summary>
class Style : IReadOnlyStyle
{
    public Visibility Visibility { get; set; } = Visibility.Visible; // CSS Inherited = YES

    public float? FontSize { get; set; } // CSS Inherited = YES

    public Color Background { get; set; } = Color.Transparent;
    public Color Foreground { get; set; } = Color.White; // CSS Inherited = YES

    public Outline Outline { get; set; } = Outline.None;
    public Border Border { get; set; } = Border.None;
    public Spacing Padding { get; set; } = Spacing.None;
    public Spacing Margin { get; set; } = Spacing.None;

    public Alignment HorizontalAlignment { get; set; } = Alignment.Auto;
    public Alignment VerticalAlignment { get; set; } = Alignment.Auto;

    public Measure? Width { get; set; }
    public Measure? Height { get; set; }
    public Measure? MinWidth { get; set; }
    public Measure? MinHeight { get; set; }
    public Measure? MaxWidth { get; set; }
    public Measure? MaxHeight { get; set; }
}


public class GuiElement
{
    public Identity Id { get; init; }

    readonly Logging.Logger logger;

    Vector2 actualOutline;
    Vector4 actualBorder;
    Vector4 actualPadding;
    Vector4 actualMargin;

    /*
    :enabled
    :disabled
    :hover
    :active (e.g. clicked)
    :checked
    :invalid
    :focus
    :read-only
    */

    /// <summary>
    /// This is the current style of the element. It is the base style with all relevant partial styles applied.
    /// </summary>
    readonly Style currentStyle = new();

    PartialStyle baseStyle = new();
    PartialStyle? enabledStyle;
    PartialStyle? disabledStyle;
    PartialStyle? hoveredStyle;
    //PrimordialStyle? activeStyle;
    //PrimordialStyle? checkedStyle;
    //PrimordialStyle? invalidStyle;
    PartialStyle? focusedStyle;
    PartialStyle? focusedVisibleStyle;

    internal Gui gui;

    internal GuiElement? parent;

    internal GuiElement[] descendants = [];

    Rectangle totalLayoutRectangle;
    Rectangle outerLayoutRectangle;
    Rectangle innerLayoutRectangle;

    public Rectangle ActualArea => totalLayoutRectangle;

    bool enabled = true;
    bool focusable = true;

    public bool isGrid = false;

    /// <summary>
    /// Gets the current style as read-only. Modifying the style directly is undefined behavior.
    /// <para> Sets the base style. Setting the style triggers a layout update. </para>
    /// </summary>
    public IReadOnlyStyle Style
    {
        get => currentStyle;
        set
        {
            baseStyle = PartialStyle.FromStyle(value);
            gui.ScheduleLayout();
        }
    }

    /// <summary>
    /// Gets or sets the style when the element is enabled.
    /// <para> Setting the style triggers a layout update. </para>
    /// </summary>
    public PartialStyle? StyleWhenEnabled
    {
        get => enabledStyle;
        set
        {
            enabledStyle = value;
            gui.ScheduleLayout();
        }
    }

    /// <summary>
    /// Gets or sets the style when the element is disabled.
    /// <para> Setting the style triggers a layout update. </para>
    /// </summary>
    public PartialStyle? StyleWhenDisabled
    {
        get => disabledStyle;
        set
        {
            disabledStyle = value;
            gui.ScheduleLayout();
        }
    }

    /// <summary>
    /// Gets or sets the style when the element is hovered.
    /// <para> Setting the style triggers a layout update. </para>
    /// </summary>
    public PartialStyle? StyleWhenHovered
    {
        get => hoveredStyle;
        set
        {
            hoveredStyle = value;
            gui.ScheduleLayout();
        }
    }

    /// <summary>
    /// Gets or sets the style when the element is focused.
    /// <para> Setting the style triggers a layout update. </para>
    /// </summary>
    public PartialStyle? StyleWhenFocused
    {
        get => focusedStyle;
        set
        {
            focusedStyle = value;
            gui.ScheduleLayout();
        }
    }

    /// <summary>
    /// Gets or sets the style when the element is focused in a visible way.
    /// <para> Setting the style triggers a layout update. </para>
    /// </summary>
    public PartialStyle? StyleWhenFocusedVisible
    {
        get => focusedVisibleStyle;
        set
        {
            focusedVisibleStyle = value;
            gui.ScheduleLayout();
        }
    }

    public bool Enabled
    {
        get { return enabled; }
        set
        {
            enabled = value;
            gui.ScheduleLayout();
        }
    }

    public bool Focusable
    {
        get { return focusable; }
        set
        {
            focusable = value;
            if (!focusable)
            {
                gui.RequestLoseFocus(this);
            }
            gui.ScheduleLayout();
        }
    }

    public bool HasMouseFocus
    {
        get;
        internal set;
    }

    public bool HasKeyboardFocus
    {
        get;
        internal set;
    }

    internal GuiElement(Gui gui, Identity id)
    {
        this.gui = gui;
        Id = id;

        logger = Gossamer.GetLogger($"{nameof(GuiElement)}({id})");
    }

    /// <summary>
    /// Attaches this element to a parent element. If the element already has a parent, it will be detached from it.
    /// </summary>
    /// <param name="newParent"></param>
    public void AttachTo(GuiElement newParent)
    {
        GuiElement? oldParent = parent;

        if (oldParent != null)
        {
            ArrayUtilities.Remove(ref oldParent.descendants, this);
            parent = null;
        }

        if (newParent != null)
        {
            ArrayUtilities.Append(ref newParent.descendants, this);
            parent = newParent;
        }
    }

    internal void UpdateCore(double absoluteTime, float deltaTime)
    {


        //Update(absoluteTime, deltaTime);

        for (int i = 0; i < descendants.Length; i++)
        {
            descendants[i].UpdateCore(absoluteTime, deltaTime);
        }
    }

    protected virtual Vector2 Measure(Vector2 spaceAvailable)
    {
        return spaceAvailable;
    }

    protected virtual void Arrange(Rectangle layoutRectangle)
    {

    }

    internal void ComputeStyleCore(bool forced)
    {
        StyleComputer.Apply(currentStyle, baseStyle);

        // :enabled
        if (enabled && enabledStyle != null)
        {
            StyleComputer.Apply(currentStyle, enabledStyle);
        }

        // :disabled
        if (!enabled && disabledStyle != null)
        {
            StyleComputer.Apply(currentStyle, disabledStyle);
        }

        // :hover
        if (HasMouseFocus && hoveredStyle != null)
        {
            StyleComputer.Apply(currentStyle, hoveredStyle);
        }

        // :focus
        if (HasKeyboardFocus && focusedStyle != null)
        {
            StyleComputer.Apply(currentStyle, focusedStyle);
        }

        // :focus-visible
        if (HasMouseFocus || HasKeyboardFocus)
        {
            if (focusedVisibleStyle != null)
            {
                StyleComputer.Apply(currentStyle, focusedVisibleStyle);
            }
        }

        for (int i = 0; i < descendants.Length; i++)
        {
            descendants[i].ComputeStyleCore(forced);
        }
    }

    public GridDefinition gridDefinition = new GridDefinition([], []);

    public GridPlacement gridPlacement;

    internal virtual Vector2 MeasureCore(Vector2 sizeAvailable, float emSize)
    {
        Vector2 spaceRequired = Vector2.Zero;

        spaceRequired.X += CalculateDistance(currentStyle.Margin.Left, sizeAvailable.X, emSize);
        spaceRequired.X += CalculateDistance(currentStyle.Border.Spacing.Left, sizeAvailable.X, emSize);
        spaceRequired.X += CalculateDistance(currentStyle.Padding.Left, sizeAvailable.X, emSize);

        spaceRequired.X += CalculateDistance(currentStyle.Margin.Right, sizeAvailable.X, emSize);
        spaceRequired.X += CalculateDistance(currentStyle.Border.Spacing.Right, sizeAvailable.X, emSize);
        spaceRequired.X += CalculateDistance(currentStyle.Padding.Right, sizeAvailable.X, emSize);

        spaceRequired.Y += CalculateDistance(currentStyle.Margin.Top, sizeAvailable.Y, emSize);
        spaceRequired.Y += CalculateDistance(currentStyle.Border.Spacing.Top, sizeAvailable.Y, emSize);
        spaceRequired.Y += CalculateDistance(currentStyle.Padding.Top, sizeAvailable.Y, emSize);

        spaceRequired.Y += CalculateDistance(currentStyle.Margin.Bottom, sizeAvailable.Y, emSize);
        spaceRequired.Y += CalculateDistance(currentStyle.Border.Spacing.Bottom, sizeAvailable.Y, emSize);
        spaceRequired.Y += CalculateDistance(currentStyle.Padding.Bottom, sizeAvailable.Y, emSize);

        sizeAvailable -= spaceRequired;

        if (isGrid)
        {
            // 1. Find the total space taken by non-fractional columns and rows.
            float totalFixedWidth = 0;
            float totalFixedHeight = 0;

            for (int i = 0; i < gridDefinition.Columns.Length; i++)
            {
                Measure column = gridDefinition.Columns[i];
                if (column.Unit != Unit.Fr || column.Unit != Unit.Auto)
                {
                    totalFixedWidth += CalculateDistance(column, sizeAvailable.X, emSize);
                }
                else if (column.Unit == Unit.Auto)
                {
                    // We have to ask the elements in this column for their size.
                    float maximumElementWidth = 0;

                    for (int iD = 0; iD < descendants.Length; iD++)
                    {
                        GuiElement descendant = descendants[iD];

                        // Figure out if the descendant is a factor for this column. It could span multiple columns.
                        int spanStart = descendant.gridPlacement.Column;
                        int spanEnd = descendant.gridPlacement.Column + descendant.gridPlacement.ColumnSpan;

                        if (spanStart <= i && i < spanEnd)
                        {
                            // This descendant is a factor for this column.
                            Vector2 descendantSize = descendant.MeasureCore(sizeAvailable, emSize);

                                                        maximumElementWidth = Math.Max(maximumElementWidth, descendantSize.X);
                        }

                    }
                }
            }

            for (int i = 0; i < gridDefinition.Rows.Length; i++)
            {
                Measure row = gridDefinition.Rows[i];
                if (row.Unit != Unit.Fr)
                {
                    totalFixedHeight += CalculateDistance(row, sizeAvailable.Y, emSize);
                }
            }

            float totalFrWidth = sizeAvailable.X - totalFixedWidth;
            float totalFrHeight = sizeAvailable.Y - totalFixedHeight;

            // 2. Find the total number of fractional columns and rows.
            int totalFrColumns = 0;
            int totalFrRows = 0;

            for (int i = 0; i < gridDefinition.Columns.Length; i++)
            {
                Measure column = gridDefinition.Columns[i];
                if (column.Unit == Unit.Fr)
                {
                    totalFrColumns++;
                }
            }

            for (int i = 0; i < gridDefinition.Rows.Length; i++)
            {
                Measure row = gridDefinition.Rows[i];
                if (row.Unit == Unit.Fr)
                {
                    totalFrRows++;
                }
            }

            // 3. Find the width and height of each cell.
            float[] cellWidths = new float[gridDefinition.Columns.Length];
            float[] cellHeights = new float[gridDefinition.Rows.Length];

            for (int i = 0; i < gridDefinition.Columns.Length; i++)
            {
                Measure column = gridDefinition.Columns[i];
                if (column.Unit == Unit.Fr)
                {
                    cellWidths[i] = totalFrWidth * column.Value;
                }
                else
                {
                    cellWidths[i] = CalculateDistance(column, sizeAvailable.X, emSize);
                }
            }

            for (int i = 0; i < gridDefinition.Rows.Length; i++)
            {
                Measure row = gridDefinition.Rows[i];
                if (row.Unit == Unit.Fr)
                {
                    cellHeights[i] = totalFrHeight * row.Value;
                }
                else
                {
                    cellHeights[i] = CalculateDistance(row, sizeAvailable.Y, emSize);
                }
            }

            // 4. Find the total width and height of the grid.
            float totalWidth = 0;
            float totalHeight = 0;

            for (int i = 0; i < cellWidths.Length; i++)
            {
                totalWidth += cellWidths[i];
            }

            for (int i = 0; i < cellHeights.Length; i++)
            {
                totalHeight += cellHeights[i];
            }

            spaceRequired = new(totalWidth, totalHeight);


        }

        logger.Debug($"Size required: {spaceRequired}");

        //Vector2 sizeWanted = Measure(sizeAvailable);
        Vector2 sizeWanted = spaceRequired;

        for (int i = 0; i < descendants.Length; i++)
        {
            Vector2 descendantWantedSize = descendants[i].MeasureCore(sizeAvailable, emSize);




            sizeWanted = Vector2.Max(sizeWanted, descendantWantedSize);
        }

        return sizeWanted;
    }

    internal virtual void ArrangeCore(Rectangle layoutRectangle, float emSize)
    {
        //Arrange(layoutRectangle);

        totalLayoutRectangle = layoutRectangle;

        // 1. Outline is drawn outside of the total layout rectangle so it doesn't affect the layout.
        actualOutline = new(
            CalculateDistance(currentStyle.Outline.Width, layoutRectangle.Width, emSize),
            CalculateDistance(currentStyle.Outline.Offset, layoutRectangle.Width, emSize));

        // 2. Margin goes between the total layout rectangle and the outer layout rectangle.
        actualMargin = new(
            CalculateDistance(currentStyle.Margin.Left, layoutRectangle.Width, emSize),
            CalculateDistance(currentStyle.Margin.Top, layoutRectangle.Height, emSize),
            CalculateDistance(currentStyle.Margin.Right, layoutRectangle.Width, emSize),
            CalculateDistance(currentStyle.Margin.Bottom, layoutRectangle.Height, emSize));
        outerLayoutRectangle = totalLayoutRectangle
            .Crop(actualMargin.X, actualMargin.Y, actualMargin.Z, actualMargin.W);

        // 3. Border goes between the outer and inner layout rectangles.
        actualBorder = new(
            CalculateDistance(currentStyle.Border.Spacing.Left, outerLayoutRectangle.Width, emSize),
            CalculateDistance(currentStyle.Border.Spacing.Top, outerLayoutRectangle.Height, emSize),
            CalculateDistance(currentStyle.Border.Spacing.Right, outerLayoutRectangle.Width, emSize),
            CalculateDistance(currentStyle.Border.Spacing.Bottom, outerLayoutRectangle.Height, emSize));
        innerLayoutRectangle = outerLayoutRectangle
            .Crop(actualBorder.X, actualBorder.Y, actualBorder.Z, actualBorder.W);

        // 4. Padding goes between the border and the inner layout rectangle.
        actualPadding = new(
            CalculateDistance(currentStyle.Padding.Left, innerLayoutRectangle.Width, emSize),
            CalculateDistance(currentStyle.Padding.Top, innerLayoutRectangle.Height, emSize),
            CalculateDistance(currentStyle.Padding.Right, innerLayoutRectangle.Width, emSize),
            CalculateDistance(currentStyle.Padding.Bottom, innerLayoutRectangle.Height, emSize));
        innerLayoutRectangle = innerLayoutRectangle
            .Crop(actualPadding.X, actualPadding.Y, actualPadding.Z, actualPadding.W);

        for (int i = 0; i < descendants.Length; i++)
        {
            descendants[i].ArrangeCore(innerLayoutRectangle, emSize);
        }
    }

    protected float CalculateDistance(Measure distance, float available, float emSize)
    {
        emSize = baseStyle.FontSize ?? emSize;

        return distance.Unit switch
        {
            Unit.Px => distance.Value,
            Unit.Em => distance.Value * emSize,
            Unit.Fr => distance.Value * available,
            _ => available
        };
    }

    internal void RenderCore(Gfx2DCommandBuffer cmdBuffer)
    {
        if (currentStyle.Visibility != Visibility.Visible)
        {
            return;
        }

        if (currentStyle.Outline.IsVisible && !currentStyle.Outline.Color.IsTransparent)
        {
            Color color = currentStyle.Outline.Color;

            cmdBuffer.FillRectangle(
                new Vector2(totalLayoutRectangle.Left - actualOutline.X - actualOutline.Y, totalLayoutRectangle.Top - actualOutline.X - actualOutline.Y),
                new Vector2(totalLayoutRectangle.Right + actualOutline.X + actualOutline.Y, totalLayoutRectangle.Top - actualOutline.Y),
                color);

            cmdBuffer.FillRectangle(
                new Vector2(totalLayoutRectangle.Left - actualOutline.X - actualOutline.Y, totalLayoutRectangle.Bottom + actualOutline.X + actualOutline.Y),
                new Vector2(totalLayoutRectangle.Right + actualOutline.X + actualOutline.Y, totalLayoutRectangle.Bottom + actualOutline.Y),
                color);

            cmdBuffer.FillRectangle(
                new Vector2(totalLayoutRectangle.Left - actualOutline.X - actualOutline.Y, totalLayoutRectangle.Top - actualOutline.Y),
                new Vector2(totalLayoutRectangle.Left - actualOutline.Y, totalLayoutRectangle.Bottom + actualOutline.Y),
                color);

            cmdBuffer.FillRectangle(
                new Vector2(totalLayoutRectangle.Right + actualOutline.X + actualOutline.Y, totalLayoutRectangle.Top - actualOutline.Y),
                new Vector2(totalLayoutRectangle.Right + actualOutline.Y, totalLayoutRectangle.Bottom + actualOutline.Y),
                color);
        }

        if (!currentStyle.Background.IsTransparent)
        {
            cmdBuffer.FillRectangle(outerLayoutRectangle, currentStyle.Background);
        }

        if (currentStyle.Border.Visibility != BorderVisibility.None && !currentStyle.Border.Color.IsTransparent)
        {
            Color color = currentStyle.Border.Color;

            if (currentStyle.Border.Visibility.HasFlag(BorderVisibility.Left))
            {
                cmdBuffer.FillRectangle(
                    new Vector2(outerLayoutRectangle.Left, outerLayoutRectangle.Top),
                    new Vector2(outerLayoutRectangle.Left + actualBorder.X, outerLayoutRectangle.Bottom),
                    color);
            }

            if (currentStyle.Border.Visibility.HasFlag(BorderVisibility.Top))
            {
                cmdBuffer.FillRectangle(
                    new Vector2(outerLayoutRectangle.Left, outerLayoutRectangle.Top),
                    new Vector2(outerLayoutRectangle.Right, outerLayoutRectangle.Top + actualBorder.Y),
                    color);
            }

            if (currentStyle.Border.Visibility.HasFlag(BorderVisibility.Right))
            {
                cmdBuffer.FillRectangle(
                    new Vector2(outerLayoutRectangle.Right - actualBorder.Z, outerLayoutRectangle.Top),
                    new Vector2(outerLayoutRectangle.Right, outerLayoutRectangle.Bottom),
                    color);
            }

            if (currentStyle.Border.Visibility.HasFlag(BorderVisibility.Bottom))
            {
                cmdBuffer.FillRectangle(
                    new Vector2(outerLayoutRectangle.Left, outerLayoutRectangle.Bottom - actualBorder.W),
                    new Vector2(outerLayoutRectangle.Right, outerLayoutRectangle.Bottom),
                    color);
            }
        }

        //if (descendants.Length == 0 && !currentStyle.Foreground.IsTransparent)
        //{
        //    cmdBuffer.FillRectangle(innerLayoutRectangle, currentStyle.Foreground);
        //}

        Render(cmdBuffer);

        for (int i = 0; i < descendants.Length; i++)
        {
            descendants[i].RenderCore(cmdBuffer);
        }
    }

    protected virtual void Render(Gfx2DCommandBuffer cmdBuffer)
    {
    }

    internal Vector2 WindowToElement(Vector2 windowPosition)
    {
        return windowPosition - outerLayoutRectangle.Position;
    }

    internal bool Contains(Vector2 mouseOnWindow, out GuiElement? element)
    {
        bool isTestable = (currentStyle.Visibility != Visibility.Visible) && enabled;
        if (isTestable)
        {
            if (outerLayoutRectangle.Contains(mouseOnWindow))
            {
                for (int i = descendants.Length - 1; i >= 0; i--)
                {
                    GuiElement? child = descendants[i];
                    if (child.Contains(mouseOnWindow, out element))
                    {
                        return true;
                    }
                }

                if (enabled)
                {
                    element = this;
                    return true;
                }
            }
        }

        element = null;
        return false;
    }

    internal void GotMouseFocus()
    {
        HasMouseFocus = true;
        OnMouseFocusedChanged(HasMouseFocus);

        if (hoveredStyle != null)
        {
            gui.ScheduleLayout();
        }
    }

    internal void LostMouseFocus()
    {
        HasMouseFocus = false;
        OnMouseFocusedChanged(HasMouseFocus);

        if (hoveredStyle != null)
        {
            gui.ScheduleLayout();
        }
    }

    internal void GotKeyboardFocus()
    {
        HasKeyboardFocus = true;
        OnKeyboardFocusedChanged(HasKeyboardFocus);

        if (focusedStyle != null)
        {
            gui.ScheduleLayout();
        }
    }

    internal void LostKeyboardFocus()
    {
        HasKeyboardFocus = false;
        OnKeyboardFocusedChanged(HasKeyboardFocus);

        if (focusedStyle != null)
        {
            gui.ScheduleLayout();
        }
    }

    internal protected virtual void OnKey(InputKey button, InputAction action, InputMods mod) { }
    internal protected virtual void OnCharacter(char c) { }
    internal protected virtual void OnScroll(float dx, float dy) { }
    internal protected virtual void OnMouseFocusedChanged(bool focused) { }
    internal protected virtual void OnKeyboardFocusedChanged(bool focused) { }
    /// <summary>
    /// Occurs when the visibility of this <see cref="GuiElement"/> has changed.
    /// </summary>
    /// <param name="visible">Indicates the current visibility state.</param>
    internal protected virtual void OnVisibleChanged(bool visible) { }
    internal protected virtual void OnMouseButton(Vector2 mouseOnElement, InputButton button, InputAction action, InputMods mod) { }
    internal protected virtual void OnMouseMove(Vector2 mouseOnElement) { }
}