#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;

namespace Gossamer.External.FreeType;

enum FreeTypeStatus : int
{
    Failure = -1,
    OK = 0,
    InvalidArgument = 1,
    InvalidData = 2,
}

struct FreeTypeFaceData
{
    public nint face_ptr;
    public int ascender;
    public int descender;
    public int height;
    public int glyph_count;
}

struct FreeTypeGlyphData
{
    public nint glyph_ptr;
    public nint bitmap_ptr;
    public int stride;
    public int width;
    public int height;
    public int bearing_x;
    public int bearing_y;
}

[System.Security.SuppressUnmanagedCodeSecurity]
unsafe class Api
{
    public const string BinaryName = "External/Gossamer.FreeType";
    const CallingConvention CallConvention = CallingConvention.Cdecl;

    public static void ThrowIfFailed(FreeTypeStatus error)
    {
        if (error != FreeTypeStatus.OK)
        {
            throw new InvalidOperationException(error.ToString());
        }
    }

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern FreeTypeStatus ftCreate(nint* out_library);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern FreeTypeStatus ftRelease(nint library);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern FreeTypeStatus ftCreateFace(nint in_library, nint in_data, ulong in_data_size, int in_width, int in_height, FreeTypeFaceData* out_face);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern FreeTypeStatus ftReleaseFace(nint in_face);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int ftGetCharIndex(nint in_face, int in_charcode);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern FreeTypeStatus ftCreateGlyph(nint in_face, int in_glyph_index, FreeTypeGlyphData* out_glyph_data);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern FreeTypeStatus ftReleaseGlyph(nint in_glyph);
}