using Gossamer.External.FreeType;

using static Gossamer.External.FreeType.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.BackEnd.Text;

public sealed class FontCollection : IDisposable
{
    readonly record struct FontKey(string Name, int Size);

    bool isDisposed;

    readonly Dictionary<FontKey, Font> fonts = [];

    nint freetypeReference;

    public Font GetDefaultFont()
    {
        return fonts.FirstOrDefault().Value ?? throw new InvalidOperationException("No fonts loaded.");
    }

    public bool TryGetFontOrDefault(string name, int verticalSize, out Font font)
    {
        if (!fonts.TryGetValue(new(name, verticalSize), out font!))
        {
            font = GetDefaultFont();
            return false;
        }

        return true;
    }

    ~FontCollection()
    {
        Dispose();
    }

    public unsafe Font LoadFont(string path, int horizontalSize, int verticalSize)
    {
        var fontBytes = File.ReadAllBytes(path);

        return LoadFont(Path.GetFileNameWithoutExtension(path), fontBytes, horizontalSize, verticalSize);
    }

    public Font LoadFont(string name, byte[] data, int horizontalSize, int verticalSize)
    {
        if (freetypeReference == default)
        {
            ThrowIf(FT_Init_FreeType(out nint ft) != FT_Error.Ok);
            freetypeReference = ft;
        }

        FontKey key = new(name, verticalSize);

        nint ftData = System.Runtime.InteropServices.Marshal.AllocHGlobal(data.Length);
        System.Runtime.InteropServices.Marshal.Copy(data, 0, ftData, data.Length);

        ThrowIf(FT_New_Memory_Face(freetypeReference, ftData, data.Length, 0, out nint ftFace) != FT_Error.Ok);

        Font font = new(ftData, ftFace, horizontalSize, verticalSize);
        fonts[key] = font;

        return font;
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
