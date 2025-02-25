using static Gossamer.External.FreeType.Api;

namespace Gossamer.Backend.Text;

public sealed class FontCollection : IDisposable
{
    readonly record struct FontKey(string Name, int Size);

    bool isDisposed;

    readonly Dictionary<FontKey, Font> fonts = [];
    readonly Font defaultFont;

    nint freetypeReference;

    /// <summary>
    /// Gets the built-in font.
    /// </summary>
    /// <returns></returns>
    public Font GetBuiltInFont()
    {
        return defaultFont;
    }

    /// <summary>
    /// Tries to get a font from the collection. If the font is not found, the built-in font is returned.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="verticalSize"></param>
    /// <param name="font"></param>
    public bool TryGetFontOrDefault(string name, int verticalSize, out Font font)
    {
        if (!fonts.TryGetValue(new(name, verticalSize), out font!))
        {
            font = GetBuiltInFont();
            return false;
        }

        return true;
    }

    public FontCollection()
    {
        // Load the default embedded font
        defaultFont = LoadFontFromBytes(
            "ProggyClean",
            Utilities.ReflectionUtilities.LoadEmbeddedResource("Gossamer.Backend.Text.ProggyClean.ttf"),
            32, 32);
    }

    ~FontCollection()
    {
        Dispose();
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

    /// <summary>
    /// Loads a font from a file.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="path"></param>
    /// <param name="horizontalSize"></param>
    /// <param name="verticalSize"></param>
    public Font LoadFontFromFile(string name, string path, int horizontalSize, int verticalSize)
    {
        return LoadFontFromBytes(name, File.ReadAllBytes(path), horizontalSize, verticalSize);
    }

    /// <summary>
    /// Loads a font from a byte array representing the font file.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="data"></param>
    /// <param name="horizontalSize"></param>
    /// <param name="verticalSize"></param>
    public Font LoadFontFromBytes(string name, byte[] data, int horizontalSize, int verticalSize)
    {
        if (freetypeReference == default)
        {
            ThrowIfFailed(FT_Init_FreeType(out nint ft));
            freetypeReference = ft;
        }

        FontKey key = new(name, verticalSize);

        nint ftData = System.Runtime.InteropServices.Marshal.AllocHGlobal(data.Length);
        System.Runtime.InteropServices.Marshal.Copy(data, 0, ftData, data.Length);

        ThrowIfFailed(FT_New_Memory_Face(freetypeReference, ftData, data.Length, 0, out nint ftFace));

        Font font = new(ftData, ftFace, horizontalSize, verticalSize);
        fonts[key] = font;

        return font;
    }
}
