using Gossamer.Collections;
using Gossamer.Utilities;

namespace Gossamer.Gfx.Text;

/// <summary>
/// A pre-computed block of text.
/// </summary>
public class TextLayout
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
    /// <param name="position"></param>
    /// <param name="glyph"></param>
    /// <param name="scale"></param>
    public void Append(Vector2 position, Glyph glyph, float scale)
    {
        ArrayUtilities.Reserve(ref glyphs, glyphCount + 1);
        glyphs[glyphCount++] = new TextLayoutGlyph(
            position * scale,
            new Vector2(glyph.Width, glyph.Height) * scale,
            new Vector2(glyph.U0, glyph.V0),
            new Vector2(glyph.U1, glyph.V1));
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