using System.Runtime.InteropServices;

using Gossamer.External.FreeType;
using Gossamer.External.HarfBuzz;

using static Gossamer.External.FreeType.Api;
using static Gossamer.External.HarfBuzz.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Gfx.Text;

public readonly record struct FontGlyph(int Index, float U0, float V0, float U1, float V1, int Width, int Height, int BearingX, int BearingY);

public readonly record struct ShapedGlyph(float XAdvance, float YAdvance, float XOffset, float YOffset, FontGlyph Glyph);

public sealed class Font : IDisposable
{
    public record Atlas(uint Width, uint Height, byte[] Pixels);

    bool isDisposed;

    public readonly record struct Metrics(int Ascender, int Descender, int Height);

    readonly FreeTypeFaceData ftFace;

    readonly nint hbBuffer;
    readonly nint hbFont;

    readonly Atlas atlas;

    readonly Dictionary<int, FontGlyph> glyphMap = [];
    readonly FontGlyph unknownGlyph;
    readonly FontGlyph spaceGlyph;

    readonly Metrics metrics;

    readonly int verticalSize;
    readonly string name;

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
        return atlas;
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
    public FontGlyph GetUnknownGlyph()
    {
        return unknownGlyph;
    }

    /// <summary>
    /// Retrieves the space glyph.
    /// </summary>
    public FontGlyph GetSpaceGlyph()
    {
        return spaceGlyph;
    }

    /// <summary>
    /// Retrieves a glyph by its index.
    /// </summary>
    /// <param name="index"></param>
    FontGlyph GetGlyphByIndex(int index)
    {
        return glyphMap.TryGetValue(index, out FontGlyph glyph) ? glyph : unknownGlyph;
    }

    /// <summary>
    /// Retrieves a glyph by its Unicode codepoint.
    /// </summary>
    /// <param name="codepoint"></param>
    public FontGlyph GetGlyphByCodepoint(int codepoint)
    {
        return GetGlyphByIndex(ftGetCharIndex(ftFace.face_ptr, codepoint));
    }

    internal Font(string name, FreeTypeFaceData face, int horizontalSize, int verticalSize)
    {
        this.name = name;
        this.verticalSize = verticalSize;

        ftFace = face;
        metrics = new Metrics(face.ascender, face.descender, face.height);

        hbBuffer = hb_buffer_create();
        hbFont = hb_ft_font_create_referenced(ftFace.face_ptr);
        //hb_ft_font_set_load_flags(hbFont, FT_Load.LOAD_TARGET_LCD);
        hb_ft_font_set_funcs(hbFont);
        hb_ft_font_changed(hbFont);

        atlas = BuildAtlas();

        spaceGlyph = GetGlyphByIndex(ftGetCharIndex(ftFace.face_ptr, new System.Text.Rune(' ').Value));
        if (glyphMap.ContainsKey(glyphMap.FirstOrDefault().Key))
        {
            unknownGlyph = glyphMap[glyphMap.FirstOrDefault().Key];
        }
        if (glyphMap.ContainsKey(0))
        {
            unknownGlyph = glyphMap[0];
        }
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

            hb_buffer_destroy(hbBuffer);
            hb_font_destroy(hbFont);

            ftReleaseFace(ftFace.face_ptr);
        }
    }

    public ref struct ShapeEnumerator
    {
        readonly Font font;
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
        internal ShapeEnumerator(Font font, nint shapedGlyphInfos, nint shapedGlyphPositions, int shapedGlyphCount)
        {
            this.font = font;
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

                FontGlyph glyph = font.GetGlyphByIndex((int)glyphInfo.CodepointOrIndex);

                return new ShapedGlyph(
                    XAdvance: glyphPosition.xAdvance / 64f,
                    YAdvance: glyphPosition.yAdvance / 64f,
                    XOffset: glyphPosition.xOffset / 64f,
                    YOffset: glyphPosition.yOffset / 64f,
                    Glyph: glyph);
            }
        }
    }

    public unsafe ShapeEnumerator ShapeText(ReadOnlySpan<uint> codepoints)
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

        return new ShapeEnumerator(this, shapedGlyphInfos, shapedGlyphPositions, shapedGlyphCount);
    }

    public unsafe ShapeEnumerator ShapeText(ReadOnlySpan<char> text)
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

        return new ShapeEnumerator(this, shapedGlyphInfos, shapedGlyphPositions, shapedGlyphCount);
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
                FreeTypeGlyph glyph = glyphs[i];

                int glyphWidth = glyph.Width + glyphPadding * 2;
                int glyphHeight = glyph.Height + glyphPadding * 2;

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
    /// <returns></returns>
    Atlas BuildAtlas()
    {
        const int Channels = 4;
        const int Padding = 2;

        using FreeTypeGlyphCollection glyphs = LoadGlyphs();

        int atlasSize = CalculateAtlasSize(glyphs, Padding);
        var bitmap = new byte[atlasSize * atlasSize * Channels];

        int atlasX = 0;
        int atlasY = 0;
        int maxHeight = 0;
        for (int i = 0; i < glyphs.Count; i++)
        {
            FreeTypeGlyph ftGlyph = glyphs[i];

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

            FontGlyph glyph = new(
                Index: ftGlyph.Index,
                U0: u0,
                V0: v0,
                U1: u1,
                V1: v1,
                Width: ftGlyph.Width,
                Height: ftGlyph.Height,
                BearingX: ftGlyph.BearingX,
                BearingY: ftGlyph.BearingY);
            glyphMap[glyph.Index] = glyph;

            atlasX += glyphWidthPadding * Channels;
        }

        return new Atlas((uint)atlasSize, (uint)atlasSize, bitmap);
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
}

sealed class FreeTypeGlyphCollection(FreeTypeGlyph[] glyphs) : IDisposable
{
    bool isDisposed;

    public int Count => glyphs.Length;

    public FreeTypeGlyph this[int index] => glyphs[index];

    ~FreeTypeGlyphCollection()
    {
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (!isDisposed)
        {
            isDisposed = true;

            foreach (var glyph in glyphs)
            {
                glyph.Dispose();
            }
        }
    }
}


class FreeTypeGlyph : IDisposable
{
    bool isDisposed;

    readonly FreeTypeGlyphData glyphPointer;

    public int Index { get; }

    public int Width { get; }

    public int Height { get; }

    public int BearingX { get; }

    public int BearingY { get; }

    public void ReadPixel(int x, int y, out byte r, out byte g, out byte b, out byte a)
    {
        const int channelCount = 1;

        nint data = glyphPointer.bitmap_ptr;
        int pitch = glyphPointer.stride;

        r = channelCount > 0 ? Marshal.ReadByte(data, y * pitch + x * channelCount + 0) : (byte)0;
        g = channelCount > 1 ? Marshal.ReadByte(data, y * pitch + x * channelCount + 1) : r;
        b = channelCount > 2 ? Marshal.ReadByte(data, y * pitch + x * channelCount + 2) : g;
        a = channelCount > 3 ? Marshal.ReadByte(data, y * pitch + x * channelCount + 3) : (byte)255;
    }

    unsafe public FreeTypeGlyph(FreeTypeFaceData face, int glyphIndex)
    {
        FreeTypeGlyphData glyphData;
        ftCreateGlyph(face.face_ptr, glyphIndex, &glyphData);
        glyphPointer = glyphData;

        Index = glyphIndex;
        BearingX = glyphData.bearing_x;
        BearingY = glyphData.bearing_y;
        Width = glyphData.width;
        Height = glyphData.height;
    }

    ~FreeTypeGlyph()
    {
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (!isDisposed)
        {
            isDisposed = true;

            ftReleaseGlyph(glyphPointer.glyph_ptr);
        }
    }
}