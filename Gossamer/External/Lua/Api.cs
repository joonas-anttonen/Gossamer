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
    InvalidConversion = 3,
}

readonly struct LuaState { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

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

    public static long luaPopInteger(LuaState state)
    {
        long outIntegerPtr;
        LuaStatus status = __luaPopInteger(state, &outIntegerPtr);

        ThrowIfFailed(status, nameof(luaPopInteger));

        return outIntegerPtr;
    }

    public static string? luaPopString(LuaState state)
    {
        byte* outStringPtr;
        LuaStatus status = __luaPopString(state, &outStringPtr);

        ThrowIfFailed(status, nameof(luaPopString));

        return Marshal.PtrToStringUTF8((nint)outStringPtr);
    }

    public static void luaPushString(LuaState state, string str)
    {
        Span<byte> strAsUtf8 = stackalloc byte[str.Length * 4];
        int len = Encoding.UTF8.GetBytes(str, strAsUtf8);

        fixed (byte* strPtr = strAsUtf8)
        {
            LuaStatus status = __luaPushString(state, strPtr, len);
            ThrowIfFailed(status, nameof(luaPushString));
        }
    }

    public static void luaRegisterApiFunction(LuaState state, string module, string name, delegate* unmanaged[Cdecl]<nint, int> function)
    {
        Span<byte> moduleAsUtf8 = stackalloc byte[module.Length * 4];
        int moduleLen = Encoding.UTF8.GetBytes(module, moduleAsUtf8);

        Span<byte> nameAsUtf8 = stackalloc byte[name.Length * 4];
        int nameLen = Encoding.UTF8.GetBytes(name, nameAsUtf8);

        fixed (byte* modulePtr = moduleAsUtf8)
        fixed (byte* namePtr = nameAsUtf8)
        {
            LuaStatus status = __luaRegisterApiFunction(state, modulePtr, moduleLen, namePtr, nameLen, (nint)function);
            ThrowIfFailed(status, nameof(luaRegisterApiFunction));
        }
    }

    public static LuaState luaOpen()
    {
        LuaState outState;
        LuaStatus status = __luaOpen(&outState);

        ThrowIfFailed(status, nameof(luaOpen));

        return outState;
    }

    public static void luaClose(LuaState state)
    {
        LuaStatus status = __luaClose(state);
        ThrowIfFailed(status, nameof(luaClose));
    }

    public static void luaRun(LuaState state, string code)
    {
        Span<byte> codeAsUtf8 = stackalloc byte[code.Length * 4];
        int len = Encoding.UTF8.GetBytes(code, codeAsUtf8);

        fixed (byte* codePtr = codeAsUtf8)
        {
            LuaStatus status = __luaRun(state, codePtr, len);
            ThrowIfFailed(status, nameof(luaRun));
        }
    }

    public static void luaPushTable(LuaState state)
    {
        LuaStatus status = __luaPushTable(state);
        ThrowIfFailed(status, nameof(luaPushTable));
    }

    public static void luaPushFunction(LuaState state, nint function)
    {
        LuaStatus status = __luaPushFunction(state, function);
        ThrowIfFailed(status, nameof(luaPushFunction));
    }

    public static void luaWriteTable(LuaState state)
    {
        LuaStatus status = __luaWriteTable(state);
        ThrowIfFailed(status, nameof(luaWriteTable));
    }

    public static void luaSetGlobal(LuaState state, string name)
    {
        Span<byte> nameAsUtf8 = stackalloc byte[name.Length * 4];
        int len = Encoding.UTF8.GetBytes(name, nameAsUtf8);

        fixed (byte* namePtr = nameAsUtf8)
        {
            LuaStatus status = __luaSetGlobal(state, namePtr, len);
            ThrowIfFailed(status, nameof(luaSetGlobal));
        }
    }

    public static int luaGetStackCount(LuaState state)
    {
        int outCount;
        LuaStatus status = __luaGetStackCount(state, &outCount);

        ThrowIfFailed(status, nameof(luaGetStackCount));

        return outCount;
    }

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaOpen")]
    static extern LuaStatus __luaOpen(LuaState* out_state);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaClose")]
    static extern LuaStatus __luaClose(LuaState in_state);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaRun")]
    static extern LuaStatus __luaRun(LuaState in_state, byte* in_data, int in_data_size);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaRegisterApiFunction")]
    static extern LuaStatus __luaRegisterApiFunction(LuaState in_state, byte* in_module, int in_module_size, byte* in_name, int in_name_size, nint in_function);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaPopInteger")]
    static extern LuaStatus __luaPopInteger(LuaState state, long* out_integer);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaPushString")]
    static extern LuaStatus __luaPushString(LuaState state, byte* in_string, int in_string_size);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaPushTable")]
    static extern LuaStatus __luaPushTable(LuaState state);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaPushFunction")]
    static extern LuaStatus __luaPushFunction(LuaState state, nint function);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaWriteTable")]
    static extern LuaStatus __luaWriteTable(LuaState state);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaSetGlobal")]
    static extern LuaStatus __luaSetGlobal(LuaState state, byte* in_name, int in_name_size);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaPopString")]
    static extern LuaStatus __luaPopString(LuaState state, byte** out_string);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "luaGetStackCount")]
    static extern LuaStatus __luaGetStackCount(LuaState state, int* out_count);
}