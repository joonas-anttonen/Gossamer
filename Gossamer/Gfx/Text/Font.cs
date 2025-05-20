using Gossamer.External.FreeType;

using static Gossamer.External.FreeType.Api;

namespace Gossamer.Gfx.Text;

public readonly record struct Glyph(int Index, float U0, float V0, float U1, float V1, int Width, int Height, int BearingX, int BearingY);

public sealed class Font : IDisposable
{
    public record Atlas(int Size, byte[] Pixels);

    bool isDisposed;

    public readonly record struct Metrics(int Ascender, int Descender, int Height);

    readonly FreeTypeFaceData ftFace;

    readonly TextShaper shaper;

    readonly Atlas glyphAtlas;

    readonly Glyph[] glyphMap;
    readonly Glyph unknownGlyph;

    readonly Metrics metrics;

    readonly int index;
    readonly string name;
    readonly int verticalSize;

    /// <summary>
    /// The index of the font in the global collection.
    /// </summary>
    public int Index
    {
        get => index;
    }

    /// <summary>
    /// The name of the font.
    /// </summary>
    public string Name
    {
        get => name;
    }

    /// <summary>
    /// The vertical size of the font.
    /// </summary>
    public int Size
    {
        get => verticalSize;
    }

    /// <summary>
    /// Retrieves the font's atlas.
    /// </summary>
    public Atlas GetAtlas()
    {
        return glyphAtlas;
    }

    /// <summary>
    /// Retrieves the font's metrics.
    /// </summary>
    public Metrics GetMetrics()
    {
        return metrics;
    }

    /// <summary>
    /// Retrieves the unknown glyph.
    /// </summary>
    public Glyph GetUnknownGlyph()
    {
        return unknownGlyph;
    }

    /// <summary>
    /// Returns the <see cref="TextShaper"/>.
    /// </summary>
    public TextShaper GetShaper()
    {
        return shaper;
    }

    

    /// <summary>
    /// Retrieves a glyph by its index.
    /// </summary>
    /// <param name="index"></param>
    Glyph GetGlyphByIndex(int index)
    {
        if (index < 0 || index >= glyphMap.Length)
        {
            return unknownGlyph;
        }

        return glyphMap[index];
    }

    /// <summary>
    /// Retrieves a glyph by its Unicode codepoint.
    /// </summary>
    /// <param name="codepoint"></param>
    public Glyph GetGlyphByCodepoint(int codepoint)
    {
        return GetGlyphByIndex(ftGetCharIndex(ftFace.face_ptr, codepoint));
    }

    internal Font(int index, string name, int verticalSize, FreeTypeFaceData ftFace)
    {
        this.index = index;
        this.name = name;
        this.verticalSize = verticalSize;
        this.ftFace = ftFace;

        metrics = new Metrics(ftFace.ascender, ftFace.descender, ftFace.height);

        (glyphAtlas, glyphMap) = Build();

        unknownGlyph = glyphMap[0];

        shaper = new TextShaper(this, ftFace, glyphMap);
    }

    ~Font()
    {
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (!isDisposed)
        {
            isDisposed = true;

            shaper.Dispose();

            ftReleaseFace(ftFace.face_ptr);
        }
    }

    static int CalculateAtlasSize(FreeTypeGlyphCollection glyphs, int glyphPadding)
    {
        int atlasSize = 128;

        while (true)
        {
            bool isLargeEnough = true;

            int x = 0;
            int y = 0;
            int maxHeight = 0;

            for (int i = 0; i < glyphs.Count; i++)
            {
                FreeTypeGlyph ftGlyph = glyphs[i];

                int glyphWidth = ftGlyph.Width + glyphPadding * 2;
                int glyphHeight = ftGlyph.Height + glyphPadding * 2;

                maxHeight = Math.Max(maxHeight, glyphHeight);
                if (x + glyphWidth > atlasSize)
                {
                    x = 0;
                    y += maxHeight;
                    maxHeight = glyphHeight;
                }
                if (y + glyphHeight > atlasSize)
                {
                    isLargeEnough = false;
                    break;
                }
                x += glyphWidth;
            }

            if (!isLargeEnough)
            {
                atlasSize *= 2;
            }
            else
            {
                break;
            }
        }

        return atlasSize;
    }

    /// <summary>
    /// Builds a font atlas from the full set of glyphs.
    /// </summary>
    (Atlas, Glyph[]) Build()
    {
        const int Channels = 4;
        const int Padding = 2;

        using FreeTypeGlyphCollection ftGlyphs = LoadGlyphs();

        Glyph[] glyphs = new Glyph[ftFace.glyph_count];

        int atlasSize = CalculateAtlasSize(ftGlyphs, Padding);
        var bitmap = new byte[atlasSize * atlasSize * Channels];

        int atlasX = 0;
        int atlasY = 0;
        int maxHeight = 0;
        for (int i = 0; i < ftGlyphs.Count; i++)
        {
            FreeTypeGlyph ftGlyph = ftGlyphs[i];

            int glyphWidth = ftGlyph.Width;
            int glyphHeight = ftGlyph.Height;

            int glyphWidthPadding = glyphWidth + Padding * 2;
            int glyphHeightPadding = glyphHeight + Padding * 2;

            maxHeight = Math.Max(maxHeight, glyphHeightPadding);
            // If we are out of atlas bounds, go to the next line
            if (atlasX + glyphWidthPadding * Channels > atlasSize * Channels)
            {
                atlasX = 0;
                atlasY += maxHeight;
                maxHeight = glyphHeightPadding;
            }

            // Copy glyph bitmap to atlas bitmap
            int glyphXPosInBitmap = atlasX / Channels + Padding; // in pixels
            int glyphYPosInBitmap = atlasY + Padding;

            int bitmapWidth = atlasSize;
            int bitmapChannels = Channels;

            // Copy glyph bitmap to atlas bitmap
            for (int glyphY = 0; glyphY < glyphHeight; ++glyphY)
            {
                int atlasBitmapRow = (glyphYPosInBitmap + glyphY) * bitmapWidth * bitmapChannels;
                for (int glyphX = 0; glyphX < glyphWidth; ++glyphX)
                {
                    int atlasBitmapIndex = atlasBitmapRow + glyphXPosInBitmap * bitmapChannels + glyphX * bitmapChannels;
                    ftGlyph.ReadPixel(glyphX, glyphY, out byte R, out byte G, out byte B, out byte A);
                    bitmap[atlasBitmapIndex + 0] = R;
                    bitmap[atlasBitmapIndex + 1] = G;
                    bitmap[atlasBitmapIndex + 2] = B;
                    bitmap[atlasBitmapIndex + 3] = A;
                }
            }

            // Calculate glyph position in texture coordinates
            float u0 = (float)glyphXPosInBitmap / atlasSize;
            float v0 = (float)glyphYPosInBitmap / atlasSize;
            float u1 = (float)(glyphXPosInBitmap + glyphWidth) / atlasSize;
            // HACK: Add 1 pixel to the height to prevent cutting off the bottom of some glyphs
            float v1 = (float)(glyphYPosInBitmap + glyphHeight + 1) / atlasSize;

            Glyph glyph = new(
                Index: ftGlyph.Index,
                U0: u0,
                V0: v0,
                U1: u1,
                V1: v1,
                Width: ftGlyph.Width,
                Height: ftGlyph.Height,
                BearingX: ftGlyph.BearingX,
                BearingY: ftGlyph.BearingY);
            glyphs[glyph.Index] = glyph;

            atlasX += glyphWidthPadding * Channels;
        }

        return (new Atlas(atlasSize, bitmap), glyphs);
    }

    FreeTypeGlyphCollection LoadGlyphs()
    {
        int glyphsInFace = ftFace.glyph_count;

        FreeTypeGlyph[] glyphs = new FreeTypeGlyph[glyphsInFace];
        for (int i = 0; i < glyphsInFace; i++)
        {
            glyphs[i] = new FreeTypeGlyph(ftFace, i);
        }

        return new FreeTypeGlyphCollection(glyphs);
    }

    sealed class FreeTypeGlyphCollection(FreeTypeGlyph[] glyphs) : IDisposable
    {
        public int Count => glyphs.Length;

        public FreeTypeGlyph this[int index] => glyphs[index];

        ~FreeTypeGlyphCollection()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            foreach (var glyph in glyphs)
            {
                glyph.Dispose();
            }
            glyphs = [];
        }
    }

    sealed class FreeTypeGlyph : IDisposable
    {
        FreeTypeGlyphData ftGlyphData;

        public int Index { get; }
        public int Width => ftGlyphData.width;
        public int Height => ftGlyphData.height;
        public int BearingX => ftGlyphData.bearing_x;
        public int BearingY => ftGlyphData.bearing_y;

        public unsafe void ReadPixel(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            a = *((byte*)ftGlyphData.bitmap_ptr + y * ftGlyphData.stride + x);
            r = 255;
            g = 255;
            b = 255;
        }

        public unsafe FreeTypeGlyph(FreeTypeFaceData face, int glyphIndex)
        {
            FreeTypeGlyphData glyphData;
            ThrowIfFailed(ftCreateGlyph(face.face_ptr, glyphIndex, &glyphData));
            ftGlyphData = glyphData;

            Index = glyphIndex;
        }

        ~FreeTypeGlyph()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (ftGlyphData.glyph_ptr != 0)
            {
                ftReleaseGlyph(ftGlyphData.glyph_ptr);
                ftGlyphData = default;
            }
        }
    }
}