using System.Runtime.InteropServices;
using System.Text;

namespace Gossamer.Utilities;

/// <summary>
/// Encapsulates a 'safe' handle to a native resource.
/// </summary>
abstract class SafeHandle : IDisposable
{
    public bool IsInvalid => handle == nint.Zero;

    protected nint handle;

    public nint DangerousGetHandle() => handle;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseHandle();
    }

    protected abstract void ReleaseHandle();

    ~SafeHandle()
    {
        Dispose(false);
    }
}

sealed class SafeNativeBlob : SafeHandle
{
    public int Size { get; }

    public SafeNativeBlob(byte[] data)
    {
        Size = sizeof(byte) * data.Length;
        handle = Marshal.AllocHGlobal(Size);
        Marshal.Copy(data, 0, handle, data.Length);
    }

    protected override void ReleaseHandle()
    {
        if (handle != nint.Zero)
        {
            Marshal.FreeHGlobal(handle);
            handle = nint.Zero;
        }
    }
}

/// <summary>
/// Encapsulates a 'safe' handle to a native string with a specified encoding.
/// </summary>
sealed class SafeNativeString : SafeHandle
{
    /// <summary>
    /// The encoding of the string.
    /// </summary>
    public Encoding Encoding { get; }

    /// <summary>
    /// Creates a new instance of <see cref="SafeNativeString"/> with the specified string and encoding. If no encoding is specified, UTF-8 is used.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="encoding"></param>
    public SafeNativeString(string str, Encoding? encoding = null)
    {
        Encoding = encoding ?? Encoding.UTF8;

        handle = StringToHGlobal(str, Encoding);
    }

    protected override void ReleaseHandle()
    {
        if (handle != nint.Zero)
        {
            Marshal.FreeHGlobal(handle);
            handle = nint.Zero;
        }
    }

    static unsafe nint StringToHGlobal(string? s, Encoding encoding)
    {
        if (s is null)
        {
            return IntPtr.Zero;
        }

        int nb = encoding.GetMaxByteCount(s.Length);

        nint ptr = Marshal.AllocHGlobal(checked(nb + 1));

        byte* pbMem = (byte*)ptr;
        int nbWritten = encoding.GetBytes(s, new Span<byte>(pbMem, nb));
        pbMem[nbWritten] = 0;

        return ptr;
    }
}

/// <summary>
/// Encapsulates a 'safe' handle to an array of native strings. Capacity is fixed and cannot be changed.
/// </summary>
sealed class SafeNativeStringArray : SafeHandle
{
    readonly SafeNativeString[] strings;

    /// <summary>
    /// The capacity of the array.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// The number of strings in the array.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Creates a new instance of <see cref="SafeNativeStringArray"/> with the specified capacity.
    /// </summary>
    /// <param name="capacity"></param>
    public SafeNativeStringArray(int capacity)
    {
        strings = new SafeNativeString[capacity];
        handle = Marshal.AllocHGlobal(capacity * nint.Size);
        Capacity = capacity;
    }

    protected override void ReleaseHandle()
    {
        if (handle != nint.Zero)
        {
            Marshal.FreeHGlobal(handle);
            handle = nint.Zero;
        }
    }

    /// <summary>
    /// Appends a new string to the array with optional encoding. If the encoding is not specified, UTF-8 is used.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="encoding"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Add(string str, Encoding? encoding = null)
    {
        if (Count >= Capacity)
        {
            throw new InvalidOperationException("Array is full.");
        }

        SafeNativeString safeNativeString = new(str, encoding);

        // Add the string to the array and write the pointer to the handle
        strings[Count] = safeNativeString;
        Marshal.WriteIntPtr(handle, Count * nint.Size, safeNativeString.DangerousGetHandle());
        Count++;
    }
}
