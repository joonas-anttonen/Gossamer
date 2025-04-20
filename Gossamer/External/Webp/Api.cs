#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;

namespace Gossamer.External.Webp;

[SuppressUnmanagedCodeSecurity]
unsafe static class Api
{
    public const string BinaryName = "External/libwebp-1.5.0";
    public const CallingConvention CallConvention = CallingConvention.Cdecl;

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int WebPGetInfo(byte* data, ulong data_size, int* width, int* height);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern byte* WebPDecodeRGB(byte* data, ulong data_size, int* width, int* height);
}