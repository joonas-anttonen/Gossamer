using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Gossamer.Utilities;

/// <summary>
/// Provides utilities for working with exceptions and assertions. Best with 'using static ExceptionUtilities'.
/// </summary>
public static class ExceptionUtilities
{
    /// <inheritdoc cref="Debug.Assert(bool, string?)"/>
    [Conditional("DEBUG")]
    public static void Assert(bool condition, string? message = default)
    {
        Debug.Assert(condition, message);
    }

    /// <inheritdoc cref="Debug.Assert(bool, string?)"/>
    [Conditional("DEBUG")]
    public static void AssertNotNull<T>([NotNull] T? instance, string? message = default) where T : class
    {
        Debug.Assert(instance != null, message);
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
    /// Throws an <see cref="NotSupportedException"/> if the value is null.
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
    /// Throws an <see cref="NotSupportedException"/> if the condition is true.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <exception cref="NotSupportedException"></exception>
    public static void ThrowNotSupportedIf(bool condition, string? message = default)
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
    /// Throws an <see cref="GossamerException"/> if the condition is true.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <exception cref="GossamerException"></exception>
    public static void ThrowIf(bool condition, string? message = default)
    {
        if (condition)
        {
            throw new GossamerException(message);
        }
    }
}