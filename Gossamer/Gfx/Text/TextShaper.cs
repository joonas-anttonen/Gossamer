using System.Text;

using Gossamer.Collections;
using Gossamer.External.FreeType;
using Gossamer.External.HarfBuzz;

using static Gossamer.External.HarfBuzz.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Gfx.Text;

public readonly record struct ShapedGlyph(float XAdvance, float YAdvance, float XOffset, float YOffset, Glyph Glyph);

public sealed class TextShaper : IDisposable
{
    const uint spaceCodepoint = 32;

    // OPTIMIZATION: Assumption is that text layouts are created and destroyed very frequently.
    //               Therefore, we use a pool to avoid unnecessary allocations.
    readonly ConcurrentObjectPool<TextLayout> textLayoutPool = new(initialCapacity: 16);

    readonly uint[] scratchRunes = new uint[1024];
    readonly Range[] scratchWordRanges = new Range[1024];

    readonly Glyph[] glyphs;

    readonly nint hbBuffer;
    readonly nint hbFont;

    readonly Font font;
    readonly float ascender;
    readonly float lineHeight;
    readonly float spaceWidth;

    internal TextShaper(Font font, FreeTypeFaceData ftFace, Glyph[] glyphs)
    {
        this.font = font;
        this.glyphs = glyphs;

        Font.Metrics fontMetrics = font.GetMetrics();
        ascender = fontMetrics.Ascender;
        lineHeight = fontMetrics.Height;

        hbBuffer = hb_buffer_create();
        hbFont = hb_ft_font_create_referenced(ftFace.face_ptr);
        //hb_ft_font_set_load_flags(hbFont, FT_Load.LOAD_TARGET_LCD);
        hb_ft_font_set_funcs(hbFont);
        hb_ft_font_changed(hbFont);

        char spaceChar = ' ';
        unsafe
        {
            // Measure the width of a space character to use as a separator between words
            spaceWidth = MeasureTextWidth(new ReadOnlySpan<char>(&spaceChar, 1));
        }
    }

    public void Dispose()
    {
        hb_buffer_destroy(hbBuffer);
        hb_font_destroy(hbFont);
    }

    public ref struct ShapeEnumerator
    {
        readonly Glyph[] glyphMap;
        readonly nint shapedGlyphInfos;
        readonly nint shapedGlyphPositions;
        readonly int shapedGlyphCount;

        /// <summary>The next index to yield.</summary>
        int _index;

        /// <summary>
        /// Returns this instance as an enumerator.
        /// </summary>
        public readonly ShapeEnumerator GetEnumerator() => this;

        /// <summary>Initialize the enumerator.</summary>
        /// <param name="span">The span to enumerate.</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal ShapeEnumerator(Glyph[] glyphMap, nint shapedGlyphInfos, nint shapedGlyphPositions, int shapedGlyphCount)
        {
            this.glyphMap = glyphMap;
            this.shapedGlyphInfos = shapedGlyphInfos;
            this.shapedGlyphPositions = shapedGlyphPositions;
            this.shapedGlyphCount = shapedGlyphCount;
            _index = -1;
        }

        /// <summary>Advances the enumerator to the next element of the span.</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = _index + 1;
            if (index < shapedGlyphCount)
            {
                _index = index;
                return true;
            }

            return false;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        readonly unsafe (hb_glyph_info_t, hb_glyph_position_t) ReadShapedGlyph(int i)
        {
            hb_glyph_info_t* pGlyphInfo = (hb_glyph_info_t*)(shapedGlyphInfos + i * sizeof(hb_glyph_info_t));
            hb_glyph_position_t* pGlyphPosition = (hb_glyph_position_t*)(shapedGlyphPositions + i * sizeof(hb_glyph_position_t));
            return (*pGlyphInfo, *pGlyphPosition);
        }

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        public ShapedGlyph Current
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                (hb_glyph_info_t glyphInfo, hb_glyph_position_t glyphPosition) = ReadShapedGlyph(_index);

                Glyph glyph = glyphMap[(int)glyphInfo.CodepointOrIndex];

                return new ShapedGlyph(
                    XAdvance: glyphPosition.xAdvance / 64f,
                    YAdvance: glyphPosition.yAdvance / 64f,
                    XOffset: glyphPosition.xOffset / 64f,
                    YOffset: glyphPosition.yOffset / 64f,
                    Glyph: glyph);
            }
        }
    }

    unsafe ShapeEnumerator ShapeText(ReadOnlySpan<uint> codepoints)
    {
        hb_buffer_clear_contents(hbBuffer);

        fixed (uint* pCodepoints = codepoints)
        {
            hb_buffer_add_codepoints(hbBuffer, pCodepoints, codepoints.Length, 0, codepoints.Length);
            hb_buffer_guess_segment_properties(hbBuffer);
        }

        hb_feature_t enableKerning = hb_feature_t.EnableKerning;
        hb_shape(hbFont, hbBuffer, (nint)(&enableKerning), 1);

        var shapedGlyphInfos = hb_buffer_get_glyph_infos(hbBuffer, out int infosLength);
        var shapedGlyphPositions = hb_buffer_get_glyph_positions(hbBuffer, out int positionsLength);
        var shapedGlyphCount = infosLength;

        return new ShapeEnumerator(glyphs, shapedGlyphInfos, shapedGlyphPositions, shapedGlyphCount);
    }

    unsafe ShapeEnumerator ShapeText(ReadOnlySpan<char> text)
    {
        hb_buffer_clear_contents(hbBuffer);

        fixed (char* pText = text)
        {
            hb_buffer_add_utf16(hbBuffer, (ushort*)pText, text.Length, 0, text.Length);
            hb_buffer_guess_segment_properties(hbBuffer);
        }

        hb_feature_t enableKerning = hb_feature_t.EnableKerning;
        hb_shape(hbFont, hbBuffer, (nint)(&enableKerning), 1);

        var shapedGlyphInfos = hb_buffer_get_glyph_infos(hbBuffer, out int infosLength);
        var shapedGlyphPositions = hb_buffer_get_glyph_positions(hbBuffer, out int positionsLength);
        var shapedGlyphCount = infosLength;

        return new ShapeEnumerator(glyphs, shapedGlyphInfos, shapedGlyphPositions, shapedGlyphCount);
    }

    float MeasureTextWidth(ReadOnlySpan<char> text)
    {
        float width = 0;

        foreach (var shapedGlyph in ShapeText(text))
        {
            Glyph glyph = shapedGlyph.Glyph;
            width += shapedGlyph.XAdvance + glyph.BearingX;
        }

        return width;
    }

    /// <summary>
    /// Computes the layout of a text string using the specified font and available size. Layouts are returned from a pool to avoid unnecessary allocations.
    /// <para>This is NOT thread safe.</para>
    /// </summary>
    /// <param name="text"></param>
    /// <param name="scale"></param>
    /// <param name="availableSize"></param>
    /// <param name="wordWrap"></param>
    public TextLayout CreateTextLayout(ReadOnlySpan<char> text, float scale, Vector2 availableSize, bool wordWrap)
    {
        TextLayout layout = textLayoutPool.Rent();
        layout.Font = font;

        availableSize *= 1 / scale;

        float cursorY = ascender;
        float totalWidth = 0;
        float totalHeight = 0;
        int wordCountOnLine = 0;
        int scratchRunesCount = 0;
        float availableWidth = availableSize.X;

        // 1. Split the text into lines
        foreach (ReadOnlySpan<char> line in text.EnumerateLines())
        {
            float remainingWidth = availableWidth;

            // 2. Split the line into "words" (ranges of characters separated by spaces)
            int wordCount = line.Split(scratchWordRanges.AsSpan(), ' ', StringSplitOptions.None);
            ThrowNotSupportedIf(wordCount >= scratchWordRanges.Length, "Too many words in a line");

            // 3. For each word, try to fit it on the current line
            //    Without word wrapping, word will always fit on the line
            //    With word wrapping, any word that overflows the line will begin a new line
            for (int i = 0; i < wordCount; i++)
            {
                bool spaceAfterWord = i > 0;

                ReadOnlySpan<char> word = line[scratchWordRanges[i]];
                float wordWidth = MeasureTextWidth(word);

                if (spaceAfterWord)
                {
                    wordWidth += spaceWidth;
                }

                bool wordOverflowsLine = remainingWidth < wordWidth;
                if (wordOverflowsLine && wordWrap)
                {
                    // 3.1 Word overflows the line, start a new line and place the word there
                    if (wordCountOnLine > 0)
                    {
                        OutputGlyphs();

                        remainingWidth = availableWidth;
                        cursorY += lineHeight;

                        AppendWord(word);

                        remainingWidth -= wordWidth;
                        wordCountOnLine = 1;
                    }
                    // 3.2 Word is too long to fit on a single line, place it on the current line anyway
                    //     Maybe in the future we can split the word into multiple lines
                    else
                    {
                        if (spaceAfterWord)
                        {
                            AppendSpace();
                        }

                        AppendWord(word);
                        OutputGlyphs();

                        remainingWidth = availableWidth;
                        cursorY += lineHeight;
                        wordCountOnLine = 0;
                    }
                }
                else
                {
                    // 3.3 Word fits on the line or no word wrapping, append to current line
                    if (spaceAfterWord)
                    {
                        AppendSpace();
                    }

                    AppendWord(word);

                    remainingWidth -= wordWidth;
                    wordCountOnLine++;
                }
            }

            OutputGlyphs();
            cursorY += lineHeight;
        }

        layout.Size = new(totalWidth, totalHeight);
        layout.Size *= scale;
        return layout;

        void AppendSpace()
        {
            scratchRunes[scratchRunesCount++] = spaceCodepoint;
        }

        void AppendWord(ReadOnlySpan<char> word)
        {
            foreach (Rune rune in word.EnumerateRunes())
            {
                scratchRunes[scratchRunesCount++] = (uint)rune.Value;
            }
        }

        void OutputGlyphs()
        {
            float cursorX = 0;

            foreach (var shapedGlyph in ShapeText(scratchRunes.AsSpan(0, scratchRunesCount)))
            {
                Glyph glyph = shapedGlyph.Glyph;

                float x = cursorX + shapedGlyph.XOffset + glyph.BearingX;
                float y = cursorY + shapedGlyph.YOffset - glyph.BearingY;

                cursorX += shapedGlyph.XAdvance;

                // Expand total size of the layout based on the bottom right corner of the glyph
                float farX = x + glyph.Width;
                float farY = y + glyph.Height;
                totalWidth = Math.Max(totalWidth, farX);
                totalHeight = Math.Max(totalHeight, farY);

                // OPTIMIZATION: Only append the glyph if it is within the available size
                bool isWithinBounds = x <= availableSize.X && y <= availableSize.Y;
                if (isWithinBounds)
                {
                    layout.Append(new Vector2(x, y), glyph, scale);
                }
            }

            scratchRunesCount = 0;
        }
    }

    /// <summary>
    /// Releases a <see cref="TextLayout"/> back to the pool.
    /// </summary>
    /// <param name="layout"></param>
    public void ReleaseTextLayout(TextLayout layout)
    {
        layout.Reset();
        textLayoutPool.Return(layout);
    }
}