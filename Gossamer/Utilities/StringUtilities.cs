namespace Gossamer;

/// <summary>
/// Collection of string utilities.
/// </summary>
public static class StringUtilities
{
    static readonly System.Globalization.NumberFormatInfo StringifyNumberFormat = new() { NumberGroupSeparator = " " };

    /// <summary>
    /// Formats the specified string using the invariant culture.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static string FormatInvariant(string format, params object?[] args)
        => string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);

    /// <summary>
    /// Converts the specified count to a user-friendly format with thousand separators. <br/>
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static string Count(ulong n)
        => n.ToString("N0", StringifyNumberFormat);

    /// <summary>
    /// Returns a debug name for the specified variable.
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <param name="variableName">The name of the variable.</param>
    /// <returns>A debug name in the format "TypeName::VariableName".</returns>
    public static string DebugName<T>(string variableName)
        => $"{typeof(T).Name}::{variableName}";

    /// <summary>
    /// Ensures that the specified path uses all forward slashes.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string SanitizePath(string path)
        => path.Replace('\\', '/');

    /// <summary>
    /// Converts the specified time span to a user-friendly format.
    /// </summary>
    /// <param name="time">The time span to convert.</param>
    /// <returns>A string representation of the time span in a user-friendly format.</returns>
    public static string TimeShort(TimeSpan time)
        => TimeShort(time.TotalSeconds);

    /// <summary>
    /// Converts the specified number of seconds to a user-friendly format.
    /// </summary>
    /// <param name="seconds">The number of seconds to convert.</param>
    /// <returns>A string representation of the seconds in a user-friendly format.</returns>
    public static string TimeShort(double seconds)
    {
        if (seconds >= 3600) return $"{seconds / 3600:F1} h";
        else if (seconds >= 60) return $"{seconds / 60:F1} m";
        else if (seconds >= 1) return $"{seconds:F1} s";
        else if (seconds >= 1e-3) return $"{seconds * 1e3:F0} ms";
        else return $"{seconds * 1e6:F0} μs";
    }

    /// <summary>
    /// Converts the specified number of seconds to a user-friendly format using the invariant culture.
    /// </summary>
    /// <param name="seconds">The number of seconds to convert.</param>
    /// <returns>A string representation of the seconds in a user-friendly format using the invariant culture.</returns>
    public static string TimeShortInvariant(double seconds)
    {
        if (seconds >= 3600) return FormatInvariant("{0:F1} h", seconds / 3600);
        else if (seconds >= 60) return FormatInvariant("{0:F1} m", seconds / 60);
        else if (seconds >= 1) return FormatInvariant("{0:F1} s", seconds);
        else if (seconds >= 1e-3) return FormatInvariant("{0:F0} ms", seconds * 1e3);
        else return FormatInvariant("{0:F0} μs", seconds * 1e6);
    }

    /// <summary>
    /// Converts the specified count to a user-friendly format with order-of-magnitude suffix. <br/>
    /// 100123 converts to 100 k. <br/>
    /// 1234 convert to 1.2 k
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static string CountShort(double n)
    {
        if (n >= 1e10) return $"{n / 1e9:F0} G";
        else if (n >= 1e9) return $"{n / 1e9:F1} G";
        else if (n >= 1e7) return $"{n / 1e6:F0} M";
        else if (n >= 1e6) return $"{n / 1e6:F1} M";
        else if (n >= 1e4) return $"{n / 1e3:F0} k";
        else if (n >= 1e3) return $"{n / 1e3:F1} k";
        else return n.ToString();
    }

    /// <summary>
    /// Converts to specified number of bytes to a user-friendly format.
    /// This is the IEC 80000-13 version, where 1 KB = 1024 B.
    /// </summary>
    /// <param name="bytes">Number of bytes.</param>
    public static string ByteSizeShortIEC(ulong bytes)
    {
        const long KiloByte = 1024;
        const long MegaByte = 1024 * 1024;
        const long GigaByte = MegaByte * 1024;

        if (bytes >= GigaByte)
            return $"{((double)bytes) / GigaByte:F2} GiB";
        else if (bytes >= MegaByte)
            return $"{((double)bytes) / MegaByte:F2} MiB";
        else if (bytes >= KiloByte)
            return $"{((double)bytes) / KiloByte:F2} KiB";
        else
            return $"{bytes} B";
    }

    /// <summary>
    /// Converts to specified number of bytes to a user-friendly format.
    /// This the SI version, where 1 KB = 1000 B.
    /// </summary>
    /// <param name="bytes">Number of bytes.</param>
    public static string ByteSizeShortSI(ulong bytes)
    {
        const long KiloByte = 1000;
        const long MegaByte = 1000 * 1000;
        const long GigaByte = MegaByte * 1000;

        if (bytes >= GigaByte)
            return $"{((double)bytes) / GigaByte:F2} GB";
        else if (bytes >= MegaByte)
            return $"{((double)bytes) / MegaByte:F2} MB";
        else if (bytes >= KiloByte)
            return $"{((double)bytes) / KiloByte:F2} KB";
        else
            return $"{bytes} B";
    }

    /// <summary>
    /// Converts the specified <see cref="Guid"/> to string and returns the last 12 characters.
    /// </summary>
    /// <param name="guid">The guid.</param>
    public static string GuidShort(Guid guid)
    {
        string str = guid.ToString();
        return str[^12..];
    }
}