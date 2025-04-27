using Gossamer.External.FreeType;

using static Gossamer.External.FreeType.Api;

namespace Gossamer.Gfx.Text;

public unsafe sealed class FontCollection : IDisposable
{
    readonly record struct FontKey(string Name, int Size);

    readonly Dictionary<string, (nint Pointer, int Length)> fontData = [];
    readonly Dictionary<FontKey, Font> fonts = [];
    readonly Font defaultFont;

    nint freetypeReference;

    /// <summary>
    /// Gets the built-in font.
    /// </summary>
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
    public bool TryGetFont(string name, int verticalSize, out Font? font)
    {
        if (!fonts.TryGetValue(new FontKey(name, verticalSize), out font))
        {
            return false;
        }

        return true;
    }

    public bool TryCreateFont(string nameOrPath, int verticalSize, out Font? font)
    {
        string pureName = Path.GetFileNameWithoutExtension(nameOrPath);

        if (fontData.TryGetValue(pureName, out var data))
        {
            font = LoadFontFromHGlobal(pureName, data.Pointer, data.Length, verticalSize, verticalSize);
            return true;
        }

        if (File.Exists(nameOrPath))
        {
            font = LoadFontFromFile(pureName, nameOrPath, verticalSize, verticalSize);
            return true;
        }
        else
        {
            nameOrPath = Path.GetFileNameWithoutExtension(nameOrPath);

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), nameOrPath + ".ttf");

                if (File.Exists(fontPath))
                {
                    font = LoadFontFromFile(pureName, fontPath, verticalSize, verticalSize);
                    return true;
                }

                fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "microsoft/windows/fonts", nameOrPath + ".ttf");

                if (File.Exists(fontPath))
                {
                    font = LoadFontFromFile(pureName, fontPath, verticalSize, verticalSize);
                    return true;
                }
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                string fontPath = Path.Combine("/usr/share/fonts", nameOrPath + ".ttf");

                if (File.Exists(fontPath))
                {
                    font = LoadFontFromFile(pureName, fontPath, verticalSize, verticalSize);
                    return true;
                }
            }
        }

        font = null;
        return false;
    }

    public FontCollection()
    {
        // Load the default embedded font
        defaultFont = LoadFontFromBytes(
            "ProggyTiny",
            Utilities.ReflectionUtilities.LoadEmbeddedResourceAsBytes("Gossamer.Gfx.Text.ProggyTiny.ttf"),
            32, 32);
    }

    ~FontCollection()
    {
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var font in fonts.Values)
        {
            font.Dispose();
        }
        fonts.Clear();

        foreach ((nint Pointer, int Length) in fontData.Values)
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(Pointer);
        }
        fontData.Clear();

        ftRelease(freetypeReference);
        freetypeReference = default;
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
        nint nativeData = System.Runtime.InteropServices.Marshal.AllocHGlobal(data.Length);
        System.Runtime.InteropServices.Marshal.Copy(data, 0, nativeData, data.Length);

        fontData.Add(name, (nativeData, data.Length));

        return LoadFontFromHGlobal(name, nativeData, data.Length, horizontalSize, verticalSize);
    }

    Font LoadFontFromHGlobal(string name, nint data, int dataLength, int horizontalSize, int verticalSize)
    {
        if (freetypeReference == default)
        {
            nint ft;
            ThrowIfFailed(ftCreate(&ft));
            freetypeReference = ft;
        }

        FreeTypeFaceData ftFace = default;
        ThrowIfFailed(ftCreateFace(freetypeReference, data, (ulong)dataLength, horizontalSize, verticalSize, &ftFace));

        Font font = new(fonts.Count, name, verticalSize, ftFace);
        FontKey key = new(name, verticalSize);

        fonts.Add(key, font);

        return font;
    }
}