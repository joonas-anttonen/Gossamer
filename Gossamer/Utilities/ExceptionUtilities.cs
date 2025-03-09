using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Gossamer.Utilities;

/// <summary>
/// Provides utilities for working with exceptions and assertions. Best with 'using static ExceptionUtilities'.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ExceptionUtilities
{
    /// <inheritdoc cref="Debug.Assert(bool, string?)"/>
    [Conditional("DEBUG")]
    public static void Assert([DoesNotReturnIf(false)] bool condition, string? message = default)
    {
        Debug.Assert(condition, message);
    }

    /// <inheritdoc cref="Debug.Assert(bool, string?)"/>
    [Conditional("DEBUG")]
    public static void AssertNotNull<T>([NotNull] T? instance, string? message = default) where T : class
    {
        Debug.Assert(instance != null, message);
    }

    /// <inheritdoc cref="Debug.Assert(bool, string?)"/>
    [Conditional("DEBUG")]
    public static void AssertIsNull<T>(T? instance, string? message = default) where T : class
    {
        Debug.Assert(instance == null, message);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the string is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="message"></param>
    /// <exception cref="ArgumentException"></exception>
    public static string ThrowArgumentIfNullOrEmpty([NotNull] string? value, string? message = default)
    {
        return string.IsNullOrEmpty(value) ? throw new ArgumentException(message) : value;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the value is null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="message"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static T ThrowArgumentNullIfNull<T>([NotNull] T? value, string? message = default) where T : class
    {
        return value ?? throw new ArgumentNullException(message);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the condition is true.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <exception cref="InvalidDataException"></exception>
    public static void ThrowInvalidDataIf(bool condition, string? message = default)
    {
        if (condition)
        {
            throw new InvalidDataException(message);
        }
    }

    /// <summary>
    /// Throws an <see cref="InvalidDataException"/> if the value is null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static T ThrowInvalidDataIfNull<T>([NotNull] T? value, string? message = default) where T : class
    {
        return value ?? throw new InvalidDataException(message);
    }

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/> if the value is null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static T ThrowNotSupportedIfNull<T>([NotNull] T? value, string? message = default) where T : class
    {
        return value ?? throw new NotSupportedException(message);
    }

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/> if the condition is true.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <exception cref="NotSupportedException"></exception>
    public static void ThrowNotSupportedIf([DoesNotReturnIf(false)] bool condition, string? message = default)
    {
        if (condition)
        {
            throw new NotSupportedException(message);
        }
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the condition is true.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void ThrowInvalidOperationIf(bool condition, string? message = default)
    {
        if (condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the value is null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="message"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static T ThrowInvalidOperationIfNull<T>([NotNull] T? value, string? message = default) where T : class
    {
        return value ?? throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Throws an <see cref="Exception"/> if the condition is true.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <exception cref="Exception"></exception>
    public static void ThrowIf(bool condition, string? message = default)
    {
        if (condition)
        {
            throw new Exception(message);
        }
    }
}