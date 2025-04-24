#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;

using Gossamer.Gfx;

namespace Gossamer.External.Webp;

enum WebPStatus : int
{
    OK,
    InvalidArgument = 1,
    InvalidData = 2,
}

enum WebPFormat : int
{
    RGBA = 0,
    BGRA = 1,
}

[SuppressUnmanagedCodeSecurity]
unsafe static class Api
{
    public const string BinaryName = "External/Gossamer.WebP";
    public const CallingConvention CallConvention = CallingConvention.Cdecl;

    public static void ThrowIfFailed(WebPStatus status, string functionName)
    {
        switch (status)
        {
            case WebPStatus.OK:
                return;
            case WebPStatus.InvalidArgument:
                throw new ArgumentException($"{functionName}: Invalid argument");
            case WebPStatus.InvalidData:
                throw new InvalidDataException($"{functionName}: Invalid data");
            default:
                throw new ExternalException($"{functionName}: {status}");
        }
    }

    public static WebPFormat webpConvertFormat(GfxFormat format)
    {
        return format switch
        {
            GfxFormat.Rgba8 => WebPFormat.RGBA,
            GfxFormat.Bgra8 => WebPFormat.BGRA,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };
    }

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern WebPStatus webpVerify(byte* in_data, int in_data_size, int* width, int* height, int* has_alpha);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern WebPStatus webpDecode(byte* in_data, int in_data_size, WebPFormat out_format, byte** out_data, int* out_data_size);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern WebPStatus webpDecodeInto(byte* in_data, int in_data_size, WebPFormat out_format, int out_stride, byte* out_data, int out_data_size);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern WebPStatus webpEncode(byte* in_data, int in_data_size, WebPFormat format, int width, int height, int stride, byte** out_data, int* out_data_size);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern WebPStatus webpFree(byte* data);
}