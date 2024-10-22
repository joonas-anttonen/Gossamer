using System.Runtime.CompilerServices;

using static System.MathF;

namespace Gossamer.Utilities;

/// <summary>
/// Represents a translation and rotation in 3D space.
/// </summary>
public struct Transform
{
    /// <summary>
    /// The translation.
    /// </summary>
    public Vector3 Translation;

    /// <summary>
    /// The rotation.
    /// </summary>
    public Quaternion Rotation;

    /// <summary>
    /// Initializes a new instance of the <see cref="Transform"/> struct.
    /// </summary>
    /// <param name="translation">The translation.</param>
    /// <param name="rotation">The rotation.</param>
    public Transform(Vector3 translation, Quaternion rotation)
    {
        Translation = translation;
        Rotation = rotation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Transform"/> struct.
    /// </summary>
    /// <param name="translation">The translation.</param>
    public Transform(Vector3 translation)
    {
        Translation = translation;
        Rotation = Quaternion.Identity;
    }

    public readonly Matrix4x4 ToMatrix()
    {
        var rotation = Matrix4x4.CreateFromQuaternion(Rotation);
        rotation.Translation = Translation;
        return rotation;
    }

    public readonly Transform Inverse()
    {
        var rotation = Quaternion.Inverse(Rotation);
        var translation = Vector3.Transform(-Translation, rotation);
        return new Transform(translation, rotation);
    }

    public static Transform Interpolate(Transform a, Transform b, float t)
    {
        var translation = Vector3.Lerp(a.Translation, b.Translation, t);
        var rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t);
        return new Transform(translation, rotation);
    }

    public static Transform Multiply(Transform a, Transform b)
    {
        var translation = Vector3.Transform(b.Translation, a.Rotation) + a.Translation;
        var rotation = a.Rotation * b.Rotation;
        return new Transform(translation, rotation);
    }

    public static Transform operator *(Transform a, Transform b)
    {
        return Multiply(a, b);
    }
}

public static class MathUtilities
{
    /// <inheritdoc cref="Vector3.Dot(Vector3, Vector3)"/>
    public static float Dot(Vector3 a, Vector3 b)
        => Vector3.Dot(a, b);

    /// <inheritdoc cref="Vector3.Cross(Vector3, Vector3)"/>
    public static Vector3 Cross(Vector3 a, Vector3 b)
        => Vector3.Cross(a, b);

    /// <inheritdoc cref="Vector3.Normalize(Vector3)"/>
    public static Vector3 Normalize(Vector3 v)
        => Vector3.Normalize(v);

    /// <inheritdoc cref="Vector3.Transform(Vector3, Matrix4x4)"/>
    public static Vector3 Transform(Vector3 v, Matrix4x4 m)
        => Vector3.Transform(v, m);

    /// <inheritdoc cref="Vector3.TransformNormal(Vector3, Matrix4x4)"/>
    public static Vector3 TransformNormal(Vector3 v, Matrix4x4 m)
        => Vector3.TransformNormal(v, m);

    /// <summary> 
    /// Calculates the angle between two vectors, in radians. 
    /// </summary>
    public static float Angle(Vector2 a, Vector2 b)
        => Acos(Vector2.Dot(a, b) / (a.Length() * b.Length()));

    /// <summary> 
    /// Calculates the angle between two vectors, in radians. 
    /// </summary>
    public static float Angle(Vector3 a, Vector3 b)
        => Acos(Vector3.Dot(a, b) / (a.Length() * b.Length()));

    /// <summary>
    /// Calculates the reciprocal of a <see cref="Vector2"/>.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector2 Reciprocal(Vector2 v)
        => new(1 / v.X, 1 / v.Y);

    /// <summary>
    /// Calculates the reciprocal of a <see cref="Vector3"/>.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector3 Reciprocal(Vector3 v)
        => new(1 / v.X, 1 / v.Y, 1 / v.Z);

    /// <summary>
    /// Inverts a <see cref="Matrix4x4"/>.
    /// </summary>
    /// <param name="m">The <see cref="Matrix4x4"/> to invert.</param>
    /// <returns>The inverted <see cref="Matrix4x4"/> or the original <see cref="Matrix4x4"/> if the inversion failed.</returns>
    public static Matrix4x4 Invert(Matrix4x4 m)
        => Matrix4x4.Invert(m, out Matrix4x4 result) ? result : m;

    #region Equality

    /// <summary>
    /// Compares two <see cref="Vector3"/> instances for equality within a specified tolerance.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AlmostEqual(Vector3 x, Vector3 y, float tolerance = 1e-6f) => AlmostEqual(x.X, y.X, tolerance) && AlmostEqual(x.Y, y.Y, tolerance) && AlmostEqual(x.Z, y.Z, tolerance);

    /// <summary>
    /// Compares two <see cref="Vector2"/> instances for equality within a specified tolerance.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AlmostEqual(Vector2 x, Vector2 y, float tolerance = 1e-6f) => AlmostEqual(x.X, y.X, tolerance) && AlmostEqual(x.Y, y.Y, tolerance);

    /// <summary>
    /// Compares two <see cref="float"/> values for equality within a specified tolerance.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AlmostEqual(float x, float y, float tolerance = 1e-6f) => Abs(x - y) <= tolerance;

    #endregion

    /// <summary> 
    /// Returns the fractional part of a floating-point number.
    /// </summary>
    public static float Frac(float v)
        => v - Floor(v);

    /// <summary> 
    /// Aligns a value to the specified alignment.
    /// </summary>
    public static uint Align(uint value, uint alignment)
        => (value + alignment - 1) / alignment;

    /// <summary> 
    /// Converts degrees to radians. 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Radians(float degrees)
        => (float)(degrees * 0.01745329251994329576923690768489);

    /// <summary> 
    /// Converts radians to degrees. 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Degrees(float radians)
        => (float)(radians / 0.01745329251994329576923690768489);

    /// <summary> 
    /// Clamps the value to the specified range.
    /// </summary>
    public static int Clamp(int x, int min, int max)
        => Math.Min(Math.Max(x, min), max);

    /// <summary> 
    /// Clamps the value to the specified range. 
    /// </summary>
    public static float Clamp(float x, float min, float max)
        => Min(Max(x, min), max);

    /// <summary>
    /// Wraps the specified value into a range [min, max].
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The min.</param>
    /// <param name="max">The max.</param>
    /// <returns>Result of the wrapping.</returns>
    /// <exception cref="ArgumentException">Is thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static int Wrap(int value, int min, int max)
    {
        if (min > max)
            throw new ArgumentException(string.Format("min {0} should be less than or equal to max {1}", min, max), "min");

        // Code from http://stackoverflow.com/a/707426/1356325
        int range_size = max - min + 1;

        if (value < min)
            value += range_size * ((min - value) / range_size + 1);

        return min + (value - min) % range_size;
    }

    /// <summary>
    /// Wraps the specified value into a range [min, max].
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="min">The min.</param>
    /// <param name="max">The max.</param>
    /// <returns>Result of the wrapping.</returns>
    /// <exception cref="ArgumentException">Is thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static float Wrap(float value, float min, float max)
    {
        if (AlmostEqual(min, max)) return min;

        double mind = min;
        double maxd = max;
        double valued = value;

        if (mind > maxd)
            throw new ArgumentException(string.Format("min {0} should be less than or equal to max {1}", min, max), "min");

        var range_size = maxd - mind;
        return (float)(mind + (valued - mind) - (range_size * Math.Floor((valued - mind) / range_size)));
    }
}