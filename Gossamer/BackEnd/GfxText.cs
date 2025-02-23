using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using Gossamer.External.FreeType;
using Gossamer.External.HarfBuzz;
using Gossamer.Utilities;

using static Gossamer.External.FreeType.Api;
using static Gossamer.External.HarfBuzz.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Backend;

class TextLayout
{
    private LayoutGlyph[] _glyphs = [];
    private int _glyphCount;

    public readonly record struct LayoutGlyph(Vector2 Position, Vector2 Size, Vector2 UV0, Vector2 UV1, Color Color);

    public int GlyphCount { get => _glyphCount; set => _glyphCount = value; }

    public LayoutGlyph[] Glyphs { get => _glyphs; set => _glyphs = value; }
    public Vector2 Size { get; set; }

    public GfxFont? Font { get; set; }

    public void Append(LayoutGlyph glyph)
    {
        ArrayUtilities.Reserve(ref _glyphs, _glyphCount + 1);
        _glyphs[_glyphCount++] = glyph;
    }

    public void Reset()
    {
        Font = null;
        Size = Vector2.Zero;
        Array.Clear(_glyphs);
        _glyphCount = 0;
    }
}

public class CharacterSet
{
    public static readonly CharacterSet Full = new(true, []);
    public static readonly CharacterSet Basic = new(false, " ABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖabcdefghijklmnopqrstuvwxyzåää0123456789!@#$€£%^&*()-_=+[]{};:'\",.<>/?\\|`~".EnumerateRunes().Select(r => (uint)r.Value).ToArray());

    public bool IsFullSet;
    public uint[] Codepoints;

    public CharacterSet(uint[] codepoints) : this(false, codepoints)
    {
    }

    CharacterSet(bool isFullSet, uint[] codepoints)
    {
        IsFullSet = isFullSet;
        Codepoints = codepoints;
    }
}

public readonly record struct GfxFontAtlas(GfxFont Font, uint Width, uint Height, byte[] Pixels);

public sealed class GfxFontCache : IDisposable
{
    readonly record struct FontKey(string Name, int Size);

    bool isDisposed;

    readonly Dictionary<FontKey, GfxFont> fonts = [];

    nint freetypeReference;

    public GfxFont GetDefaultFont()
    {
        return fonts.FirstOrDefault().Value ?? throw new InvalidOperationException("No fonts loaded.");
    }

    public bool TryGetFontOrDefault(string name, int size, out GfxFont font)
    {
        if (!fonts.TryGetValue(new(name, size), out font!))
        {
            font = GetDefaultFont();
            return false;
        }

        return true;
    }

    public GfxFontCache()
    {

    }

    ~GfxFontCache()
    {
        Dispose();
    }

    public unsafe (GfxFont, GfxFontAtlas) LoadFont(string path, int pixelSize, CharacterSet characterSet)
    {
        var fontBytes = File.ReadAllBytes(path);

        return LoadFont(Path.GetFileNameWithoutExtension(path), fontBytes, pixelSize, characterSet);
    }

    public unsafe (GfxFont, GfxFontAtlas) LoadFont(string name, byte[] data, int pixelSize, CharacterSet characterSet)
    {
        FT_Error ftError;

        if (freetypeReference == default)
        {
            ftError = FT_Init_FreeType(out nint ft);
            ThrowIf(ftError != FT_Error.Ok, "Failed to initialize FreeType.");
            freetypeReference = ft;
        }

        FontKey key = new(name, pixelSize);

        nint ftData = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, ftData, data.Length);

        ftError = FT_New_Memory_Face(freetypeReference, ftData, data.Length, 0, out nint ftFace);
        ThrowIf(ftError != FT_Error.Ok, "Failed to load font.");

        GfxFont font = new(ftData, ftFace, pixelSize, characterSet);
        fonts[key] = font;

        using GfxGlyphCollection glyphs = font.LoadGlyphs();

        int atlasSize = glyphs.CalculateSizeRequired();

        return (font, new GfxFontAtlas(font, (uint)atlasSize, (uint)atlasSize, font.BuildBitmap(glyphs, atlasSize)));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (!isDisposed)
        {
            isDisposed = true;

            foreach (var font in fonts.Values)
            {
                font.Dispose();
            }

            FT_Done_FreeType(freetypeReference);
            freetypeReference = default;
        }
    }
}

public sealed class GfxGlyphCollection(FreeTypeGlyph[] glyphs) : IDisposable
{
    bool isDisposed;

    public int Count => glyphs.Length;

    public FreeTypeGlyph this[int index] => glyphs[index];

    ~GfxGlyphCollection()
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

    public int CalculateSizeRequired()
    {
        int atlasSize = 128;
        int padding = 1;

        while (true)
        {
            bool isLargeEnough = true;

            int x = 0;
            int y = 0;
            int maxHeight = 0;

            for (int i = 0; i < glyphs.Length; i++)
            {
                FreeTypeGlyph glyph = glyphs[i];

                int glyphWidth = glyph.Width + padding * 2;
                int glyphHeight = glyph.Height + padding * 2;

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
}

/// <summary>
/// Represents a glyph in a font atlas.
/// </summary>
/// <param name="Codepoint"></param>
/// <param name="Index"></param>
/// <param name="U0"></param>
/// <param name="V0"></param>
/// <param name="U1"></param>
/// <param name="V1"></param>
/// <param name="Width">Width of the glyph in pixels.</param>
/// <param name="Height">Height of the glyph in pixels.</param>
/// <param name="BearingX"></param>
/// <param name="BearingY"></param>
public readonly record struct Glyph(uint Codepoint, uint Index, float U0, float V0, float U1, float V1, int Width, int Height, int BearingX, int BearingY);

public readonly record struct ShapedGlyph(float XAdvance, float YAdvance, float XOffset, float YOffset, Glyph Glyph);

public sealed class GfxFont : IDisposable
{
    bool isDisposed;

    public readonly record struct Metrics(int Ascender, int Descender, int Height);

    readonly nint ftBlob;
    readonly nint ftFace;

    readonly nint hbBuffer;
    readonly nint hbFont;

    nint shapedGlyphInfos;
    nint shapedGlyphPositions;
    int shapedGlyphCount;

    readonly Metrics metrics;

    public Metrics GetMetrics() => metrics;

    readonly Dictionary<uint, Glyph> glyphMap = [];
    Glyph unknownGlyph;

    Glyph GetGlyphByIndex(uint index)
    {
        if (glyphMap.TryGetValue(index, out Glyph glyph))
        {
            return glyph;
        }

        return unknownGlyph;
    }

    public int PixelSize { get; }

    public CharacterSet CharacterSet { get; }

    public GfxFont(nint fontBlob, nint face, int pixelSize, CharacterSet characterSet)
    {
        CharacterSet = characterSet;

        ftBlob = fontBlob;
        ftFace = face;
        var ftError = FT_Set_Char_Size(ftFace, pixelSize * 64, pixelSize * 64, 0, 0);
        //var ftError = FT_Set_Pixel_Sizes(ftFace, 0, (uint)pixelSize);
        ThrowIf(ftError != FT_Error.Ok, "Failed to set pixel sizes.");

        hbBuffer = hb_buffer_create();
        hbFont = hb_ft_font_create_referenced(ftFace);
        hb_ft_font_set_load_flags(hbFont, FT_Load.LOAD_TARGET_LCD);
        //hb_font_set_ptem(hbFont, pixelSize * 64);

        unsafe
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FaceRec* faceData = (FaceRec*)ftFace;
                SizeRec* sizeData = (SizeRec*)faceData->size;

                metrics = new(
                    Ascender: sizeData->metrics.ascender / 64,
                    Descender: sizeData->metrics.descender / 64,
                    Height: sizeData->metrics.height / 64);
            }
            else
            {
                FaceRec64* faceData = (FaceRec64*)ftFace;
                SizeRec64* sizeData = faceData->size;

                metrics = new(
                    Ascender: (int)sizeData->metrics.ascender / 64,
                    Descender: (int)sizeData->metrics.descender / 64,
                    Height: (int)sizeData->metrics.height / 64);
            }
        }
    }

    ~GfxFont()
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

            FT_Done_Face(ftFace);

            Marshal.FreeHGlobal(ftBlob);
        }
    }

    unsafe void ShapeCodepointsHb(ReadOnlySpan<uint> codepoints)
    {
        hb_buffer_clear_contents(hbBuffer);

        fixed (uint* pCodepoints = codepoints)
        {
            hb_buffer_add_codepoints(hbBuffer, pCodepoints, codepoints.Length, 0, codepoints.Length);
            hb_buffer_guess_segment_properties(hbBuffer);
        }

        hb_shape(hbFont, hbBuffer, default, default);

        shapedGlyphInfos = hb_buffer_get_glyph_infos(hbBuffer, out int infosLength);
        shapedGlyphPositions = hb_buffer_get_glyph_positions(hbBuffer, out int positionsLength);
        shapedGlyphCount = infosLength;
    }

    unsafe void ShapeStringHb(ReadOnlySpan<char> text)
    {
        hb_buffer_clear_contents(hbBuffer);

        fixed (char* pText = text)
        {
            hb_buffer_add_utf16(hbBuffer, (ushort*)pText, text.Length, 0, text.Length);
            hb_buffer_guess_segment_properties(hbBuffer);
        }

        hb_shape(hbFont, hbBuffer, default, default);

        shapedGlyphInfos = hb_buffer_get_glyph_infos(hbBuffer, out int infosLength);
        shapedGlyphPositions = hb_buffer_get_glyph_positions(hbBuffer, out int positionsLength);
        shapedGlyphCount = infosLength;
    }

    unsafe (hb_glyph_info_t, hb_glyph_position_t) ReadShapedGlyph(int i)
    {
        hb_glyph_info_t* pGlyphInfo = (hb_glyph_info_t*)(shapedGlyphInfos + i * sizeof(hb_glyph_info_t));
        hb_glyph_position_t* pGlyphPosition = (hb_glyph_position_t*)(shapedGlyphPositions + i * sizeof(hb_glyph_position_t));
        return (*pGlyphInfo, *pGlyphPosition);
    }

    /*internal int Length => shapedGlyphCount;

    public ref struct Enumerator
    {
        /// <summary>The span being enumerated.</summary>
        private readonly GfxFont _span;
        /// <summary>The next index to yield.</summary>
        private int _index;

        /// <summary>Initialize the enumerator.</summary>
        /// <param name="span">The span to enumerate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(GfxFont span)
        {
            _span = span;
            _index = -1;
        }

        /// <summary>Advances the enumerator to the next element of the span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = _index + 1;
            if (index < _span.Length)
            {
                _index = index;
                return true;
            }

            return false;
        }

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        public ref readonly ShapedGlyph Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }
    }*/

    public IEnumerable<ShapedGlyph> ShapeText(ReadOnlySpan<uint> codepoints)
    {
        ShapeCodepointsHb(codepoints);
        return GenerateShapedGlyphs();
    }

    public IEnumerable<ShapedGlyph> ShapeText(ReadOnlySpan<char> text)
    {
        ShapeStringHb(text);
        return GenerateShapedGlyphs();
    }

    public IEnumerable<ShapedGlyph> GenerateShapedGlyphs()
    {
        for (int i = 0; i < shapedGlyphCount; i++)
        {
            (hb_glyph_info_t glyphInfo, hb_glyph_position_t glyphPosition) = ReadShapedGlyph(i);

            Glyph glyph = GetGlyphByIndex(glyphInfo.codepoint);

            yield return new ShapedGlyph(
                XAdvance: glyphPosition.xAdvance / 64f,
                YAdvance: glyphPosition.yAdvance / 64f,
                XOffset: glyphPosition.xOffset / 64f,
                YOffset: glyphPosition.yOffset / 64f,
                Glyph: glyph);
        }
    }

    public byte[] BuildBitmap(GfxGlyphCollection glyphs, int atlasSize)
    {
        const int Channels = 4;
        const int Padding = 1;

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
            if (atlasX + glyphWidthPadding * Channels > (atlasSize * Channels))
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
                    ColorRgba8 pixel = ftGlyph.ReadPixel(glyphX, glyphY);
                    bitmap[atlasBitmapIndex + 0] = pixel.R;
                    bitmap[atlasBitmapIndex + 1] = pixel.G;
                    bitmap[atlasBitmapIndex + 2] = pixel.B;
                    bitmap[atlasBitmapIndex + 3] = pixel.A;
                }
            }

            // Calculate glyph position in texture coordinates
            float u0 = (float)glyphXPosInBitmap / atlasSize;
            float v0 = (float)glyphYPosInBitmap / atlasSize;
            float u1 = (float)(glyphXPosInBitmap + glyphWidth) / atlasSize;
            float v1 = (float)(glyphYPosInBitmap + glyphHeight) / atlasSize;

            Glyph glyph = new(
                Codepoint: ftGlyph.Codepoint,
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

        if (glyphMap.ContainsKey(glyphMap.FirstOrDefault().Key))
        {
            unknownGlyph = glyphMap[glyphMap.FirstOrDefault().Key];
        }
        if (glyphMap.ContainsKey(0))
        {
            unknownGlyph = glyphMap[0];
        }

        return bitmap;
    }

    public GfxGlyphCollection LoadGlyphs()
    {
        IEnumerable<uint> codepoints;

        if (CharacterSet.IsFullSet)
        {
            codepoints = GenerateFullCharacterSet();
        }
        else
        {
            if (CharacterSet.Codepoints.Length == 0)
            {
                codepoints = Enumerable.Repeat(0xFFFFu, 1);
            }
            else if (CharacterSet.Codepoints[0] != 0xFFFFu)
            {
                codepoints = CharacterSet.Codepoints.Prepend(0xFFFFu);
            }
            else
            {
                codepoints = CharacterSet.Codepoints;
            }
        }

        return new(codepoints.Select(codepoint => new FreeTypeGlyph(ftFace, codepoint)).ToArray());
    }

    public Glyph GetGlyph(uint codepoint)
    {
        return GetGlyphByIndex(FT_Get_Char_Index(ftFace, codepoint));
    }

    IEnumerable<uint> GenerateFullCharacterSet()
    {
        yield return 0xFFFF;

        uint codepoint = FT_Get_First_Char(ftFace, out uint nextGlyphIndex);

        while (nextGlyphIndex != 0)
        {
            yield return codepoint;
            codepoint = FT_Get_Next_Char(ftFace, codepoint, out nextGlyphIndex);
        }
    }
}

public class FreeTypeGlyph : IDisposable
{
    bool isDisposed;

    readonly nint glyphPointer;
    readonly nint bitmapPointer;

    readonly PixelMode pixelMode;
    readonly int pitch;

    public uint Codepoint { get; }

    public uint Index { get; }

    public int Width { get; }

    public int Height { get; }

    public int BearingX { get; }

    public int BearingY { get; }

    public ColorRgba8 ReadPixel(int x, int y)
    {
        int rOff = pixelMode == PixelMode.Bgra ? 2 : 0;
        int gOff = pixelMode == PixelMode.Bgra || pixelMode == PixelMode.Lcd ? 1 : 0;
        int bOff = pixelMode == PixelMode.Lcd ? 2 : 0;
        int aOff = pixelMode == PixelMode.Bgra ? 3 : 0;
        int channelCount = pixelMode switch
        {
            PixelMode.Gray => 1,
            PixelMode.Lcd => 3,
            PixelMode.Bgra => 4,
            _ => 1,
        };
        byte r = 0;
        byte g = 0;
        byte b = 0;
        byte a = pixelMode == PixelMode.Lcd ? (byte)255 : (byte)0;

        r = Marshal.ReadByte(bitmapPointer, y * pitch + (x * channelCount + rOff));
        if (channelCount > 1)
            g = Marshal.ReadByte(bitmapPointer, y * pitch + (x * channelCount + gOff));
        if (channelCount > 2)
            b = Marshal.ReadByte(bitmapPointer, y * pitch + (x * channelCount + bOff));
        if (channelCount > 3)
            a = Marshal.ReadByte(bitmapPointer, y * pitch + (x * channelCount + aOff));

        return new ColorRgba8(r, g, b, a);
    }

    unsafe public FreeTypeGlyph(nint face, uint codepoint)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            FaceRec faceData = Marshal.PtrToStructure<FaceRec>(face);

            var ftError = FT_Load_Char(face, codepoint, FT_Load.LOAD_TARGET_LCD);
            ThrowIf(ftError != FT_Error.Ok, "Failed to load glyph.");
            ftError = FT_Render_Glyph(faceData.glyph, FtRenderMode.LCD);
            ThrowIf(ftError != FT_Error.Ok, "Failed to render glyph.");
            ftError = FT_Get_Glyph(faceData.glyph, out nint glyphPtr);
            ThrowIf(ftError != FT_Error.Ok, "Failed to get glyph.");

            GlyphSlotRec glyphSlotData = Marshal.PtrToStructure<GlyphSlotRec>(faceData.glyph);
            BitmapGlyphRec glyphBitmapData = Marshal.PtrToStructure<BitmapGlyphRec>(glyphPtr);

            glyphPointer = glyphPtr;
            bitmapPointer = glyphBitmapData.bitmap.buffer;

            Codepoint = codepoint;
            Index = glyphSlotData.glyph_index;

            BearingX = (int)(glyphSlotData.metrics.horiBearingX / 64);
            BearingY = (int)(glyphSlotData.metrics.horiBearingY / 64);

            if (BearingX > 10_000)
            {
                const uint V = (uint.MaxValue / 64);
                uint v = glyphSlotData.metrics.horiBearingX / 64;
                BearingX = BearingX = (int)(V - v);
            }

            BitmapRec bitmapData = glyphSlotData.bitmap;
            pixelMode = bitmapData.pixel_mode;
            pitch = bitmapData.pitch;

            Width = pixelMode == PixelMode.Lcd ? bitmapData.width / 3 : bitmapData.width;
            Height = bitmapData.rows;
        }
        else
        {
            FaceRec64 faceData = Marshal.PtrToStructure<FaceRec64>(face);

            var ftError = FT_Load_Char(face, codepoint, FT_Load.LOAD_TARGET_LCD);
            ThrowIf(ftError != FT_Error.Ok, "Failed to load glyph.");
            ftError = FT_Render_Glyph((nint)faceData.glyph, FtRenderMode.LCD);
            ThrowIf(ftError != FT_Error.Ok, "Failed to render glyph.");
            ftError = FT_Get_Glyph((nint)faceData.glyph, out nint glyphPtr);
            ThrowIf(ftError != FT_Error.Ok, "Failed to get glyph.");

            GlyphSlotRec64 glyphSlotData = *faceData.glyph;
            BitmapGlyphRec64 glyphBitmapData = Marshal.PtrToStructure<BitmapGlyphRec64>(glyphPtr);

            glyphPointer = glyphPtr;
            bitmapPointer = glyphBitmapData.bitmap.buffer;

            Codepoint = codepoint;
            Index = glyphSlotData.glyph_index;

            BearingX = (int)(glyphSlotData.metrics.horiBearingX / 64);
            BearingY = (int)(glyphSlotData.metrics.horiBearingY / 64);

            if (BearingX > 10_000)
            {
                const uint V = (uint.MaxValue / 64);
                uint v = (uint)glyphSlotData.metrics.horiBearingX / 64;
                BearingX = BearingX = (int)(V - v);
            }

            BitmapRec bitmapData = glyphSlotData.bitmap;
            pixelMode = bitmapData.pixel_mode;
            pitch = bitmapData.pitch;

            Width = pixelMode == PixelMode.Lcd ? bitmapData.width / 3 : bitmapData.width;
            Height = bitmapData.rows;
        }
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

            FT_Done_Glyph(glyphPointer);
        }
    }
}