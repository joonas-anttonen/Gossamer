#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Gossamer.External.Lua;

enum LuaStatus : int
{
    Fault = -1,
    OK,
    InvalidArgument = 1,
    InvalidData = 2,
}

[SuppressUnmanagedCodeSecurity]
unsafe static class Api
{
    public const string BinaryName = "External/Gossamer.Lua";
    public const CallingConvention CallConvention = CallingConvention.Cdecl;

    public static void ThrowIfFailed(LuaStatus status, string functionName)
    {
        switch (status)
        {
            case LuaStatus.OK:
                return;
            case LuaStatus.InvalidArgument:
                throw new ArgumentException($"{functionName}: Invalid argument");
            case LuaStatus.InvalidData:
                throw new InvalidDataException($"{functionName}: Invalid data");
            default:
                throw new ExternalException($"{functionName}: {status}");
        }
    }

    public static void luaRun(string code)
    {
        Span<byte> codeAsUtf8 = stackalloc byte[code.Length * 4];
        int len = Encoding.UTF8.GetBytes(code, codeAsUtf8);

        fixed (byte* codePtr = codeAsUtf8)
        {
            LuaStatus status = luaRun(codePtr, len);
            ThrowIfFailed(status, nameof(luaRun));
        }
    }

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern LuaStatus luaRun(byte* in_data, int in_data_size);
}