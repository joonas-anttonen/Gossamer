using System.Text;

using Gossamer.Collections;

using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Gfx.Text;

public class TextShaper
{
    // OPTIMIZATION: Assumption is that text layouts are created and destroyed very frequently.
    //               Therefore, we use a pool to avoid unnecessary allocations.
    readonly ConcurrentObjectPool<TextLayout> textLayoutPool = new(initialCapacity: 16);

    readonly uint[] scratchRunes = new uint[1024];
    readonly Range[] scratchWordRanges = new Range[1024];

    /// <summary>
    /// Computes the layout of a text string using the specified font and available size. Layouts are returned from a pool to avoid unnecessary allocations.
    /// <para>This is NOT thread safe.</para>
    /// </summary>
    /// <param name="font"></param>
    /// <param name="text"></param>
    /// <param name="availableSize"></param>
    /// <param name="wordWrap"></param>
    public TextLayout CreateTextLayout(ReadOnlySpan<char> text, Font font, Vector2 availableSize, bool wordWrap)
    {
        const int spaceCodepoint = 32;

        TextLayout layout = textLayoutPool.Rent();
        layout.Font = font;

        Font.Metrics fontMetrics = font.GetMetrics();
        float spaceRuneWidth = font.GetSpaceGlyph().Width;
        float fontLineHeight = fontMetrics.Height;
        float cursorY = fontMetrics.Ascender;
        float totalWidth = 0;
        float totalHeight = 0;
        int wordCountOnLine = 0;
        int scratchRunesCount = 0;

        // 1. Split the text into lines
        foreach (ReadOnlySpan<char> line in text.EnumerateLines())
        {
            float remainingWidth = availableSize.X;

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
                Vector2 wordSize = EstimateWordSize(word);

                if (spaceAfterWord)
                {
                    wordSize.X += spaceRuneWidth;
                }

                bool wordOverflowsLine = remainingWidth < wordSize.X;
                if (wordOverflowsLine && wordWrap)
                {
                    // 3.1 Word overflows the line, start a new line and place the word there
                    if (wordCountOnLine > 0)
                    {
                        ConsumeRunes(font, layout, cursorY);

                        remainingWidth = availableSize.X;
                        cursorY += fontLineHeight;

                        foreach (Rune rune in word.EnumerateRunes())
                        {
                            scratchRunes[scratchRunesCount++] = (uint)rune.Value;
                        }

                        wordCountOnLine = 1;
                        remainingWidth -= wordSize.X;
                    }
                    // 3.2 Word is too long to fit on a single line, place it on the current line anyway
                    //     Maybe in the future we can split the word into multiple lines
                    else
                    {
                        if (spaceAfterWord)
                        {
                            scratchRunes[scratchRunesCount++] = spaceCodepoint;
                        }

                        foreach (Rune rune in word.EnumerateRunes())
                        {
                            scratchRunes[scratchRunesCount++] = (uint)rune.Value;
                        }
                        ConsumeRunes(font, layout, cursorY);

                        remainingWidth = availableSize.X;
                        cursorY += fontLineHeight;
                        wordCountOnLine = 0;
                    }
                }
                else
                {
                    // 3.3 Word fits on the line or no word wrapping, append to current line
                    if (spaceAfterWord)
                    {
                        scratchRunes[scratchRunesCount++] = spaceCodepoint;
                    }

                    foreach (Rune rune in word.EnumerateRunes())
                    {
                        scratchRunes[scratchRunesCount++] = (uint)rune.Value;
                    }

                    remainingWidth -= wordSize.X;
                    wordCountOnLine++;
                }
            }

            if (scratchRunesCount > 0)
            {
                ConsumeRunes(font, layout, cursorY);
            }

            cursorY += fontLineHeight;
        }

        layout.Size = new(totalWidth, totalHeight);
        return layout;

        Vector2 EstimateWordSize(ReadOnlySpan<char> word)
        {
            // Empirically determined padding between glyphs to give results that are "close enough"
            // This is terrible. Replace with proper glyph spacing calculation.
            const float glyphHorizontalPadding = 4;

            float width = 0;
            float height = fontLineHeight;

            foreach (var rune in word.EnumerateRunes())
            {
                FontGlyph glyph = font.GetGlyphByCodepoint(rune.Value);

                width += glyph.Width + glyphHorizontalPadding;
            }

            return new(width, height);
        }

        void ConsumeRunes(Font font, TextLayout layout, float cursorY)
        {
            float cursorX = 0;

            foreach (var shapedGlyph in font.ShapeText(scratchRunes.AsSpan(0, scratchRunesCount)))
            {
                FontGlyph glyph = shapedGlyph.Glyph;

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
                    layout.Append(new(x, y), glyph);
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