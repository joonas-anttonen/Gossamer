#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;

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
}

[SuppressUnmanagedCodeSecurity]
unsafe static class Api
{
    public const string BinaryName = "External/Gossamer.WebP";
    public const CallingConvention CallConvention = CallingConvention.Cdecl;

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern WebPStatus Analyze(byte* in_data, ulong in_data_size, int* width, int* height, int* has_alpha);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern WebPStatus Decode(byte* in_data, ulong in_data_size, WebPFormat out_format, byte** out_data, ulong* out_data_size);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern WebPStatus Encode(byte* in_data, ulong in_data_size, WebPFormat in_format, int width, int height, byte** out_data, ulong* out_data_size);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern WebPStatus Free(byte* data);
}