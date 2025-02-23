using Gossamer.Utilities;

namespace Gossamer.BackEnd.Text;

/// <summary>
/// A pre-computed block of text.
/// </summary>
class TextLayout
{
    public readonly record struct TextLayoutGlyph(Vector2 Position, Vector2 Size, Vector2 UV0, Vector2 UV1);

    TextLayoutGlyph[] glyphs = new TextLayoutGlyph[32];
    int glyphCount;

    public int GlyphCount
    {
        get => glyphCount;
        set => glyphCount = value;
    }

    public TextLayoutGlyph[] Glyphs
    {
        get => glyphs;
        set => glyphs = value;
    }

    public Vector2 Size
    {
        get;
        set;
    }

    public Font? Font
    {
        get;
        set;
    }

    /// <summary>
    /// Appends a glyph to the text layout.
    /// </summary>
    /// <param name="glyph"></param>
    public void Append(Vector2 position, FontGlyph glyph)
    {
        ArrayUtilities.Reserve(ref glyphs, glyphCount + 1);
        glyphs[glyphCount++] = new TextLayoutGlyph(
            position,
            new(glyph.Width, glyph.Height),
            new(glyph.U0, glyph.V0),
            new(glyph.U1, glyph.V1));
    }

    /// <summary>
    /// Completely resets the state of the text layout.
    /// </summary>
    public void Reset()
    {
        Font = null;
        Size = Vector2.Zero;
        Array.Clear(glyphs);
        glyphCount = 0;
    }
}
