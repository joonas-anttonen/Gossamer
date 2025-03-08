namespace Gossamer;

/// <summary>
/// Represents a translation and rotation in 3D space.
/// </summary>
public struct Transform3D
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
    /// Initializes a new instance of the <see cref="Transform3D"/> struct.
    /// </summary>
    /// <param name="translation">The translation.</param>
    /// <param name="rotation">The rotation.</param>
    public Transform3D(Vector3 translation, Quaternion rotation)
    {
        Translation = translation;
        Rotation = rotation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Transform3D"/> struct.
    /// </summary>
    /// <param name="translation">The translation.</param>
    public Transform3D(Vector3 translation)
    {
        Translation = translation;
        Rotation = Quaternion.Identity;
    }

    /// <summary>
    /// Returns the matrix representation of this transform.
    /// </summary>
    public readonly Matrix4x4 ToMatrix()
    {
        var rotation = Matrix4x4.CreateFromQuaternion(Rotation);
        rotation.Translation = Translation;
        return rotation;
    }

    /// <summary>
    /// Returns the inverse of this transform.
    /// </summary>
    public readonly Transform3D Inverse()
    {
        var rotation = Quaternion.Inverse(Rotation);
        var translation = Vector3.Transform(-Translation, rotation);
        return new Transform3D(translation, rotation);
    }

    /// <summary>
    /// Interpolates between two transforms.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Transform3D Interpolate(Transform3D a, Transform3D b, float t)
    {
        var translation = Vector3.Lerp(a.Translation, b.Translation, t);
        var rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t);
        return new Transform3D(translation, rotation);
    }

    /// <summary>
    /// Multiplies the two transforms.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static Transform3D Multiply(Transform3D a, Transform3D b)
    {
        var translation = Vector3.Transform(b.Translation, a.Rotation) + a.Translation;
        var rotation = a.Rotation * b.Rotation;
        return new Transform3D(translation, rotation);
    }

    public static Transform3D operator *(Transform3D a, Transform3D b)
    {
        return Multiply(a, b);
    }

    /// <summary>
    /// Applies the specified transform to the given vector.
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="transform"></param>
    public static Vector3 Transform(Vector3 vector, Transform3D transform)
    {
        return Vector3.Transform(vector, transform.Rotation) + transform.Translation;
    }

    /// <summary>
    /// Applies the specified transform to the given normal vector.
    /// </summary>
    /// <param name="normal"></param>
    /// <param name="transform"></param>
    public static Vector3 TransformNormal(Vector3 normal, Transform3D transform)
    {
        return Vector3.Transform(normal, transform.Rotation);
    }
}

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class TransformExtensions
{
    public static Transform3D ToTransform(this Matrix4x4 matrix)
    {
        return new Transform3D(matrix.Translation, Quaternion.CreateFromRotationMatrix(matrix));
    }

    public static Matrix4x4 ToMatrix(this Transform3D transform)
    {
        return transform.ToMatrix();
    }

    public static Transform3D Inverse(this Transform3D transform)
    {
        return transform.Inverse();
    }

    public static Transform3D Interpolate(this Transform3D a, Transform3D b, float t)
    {
        return Transform3D.Interpolate(a, b, t);
    }

    public static Transform3D Multiply(this Transform3D a, Transform3D b)
    {
        return Transform3D.Multiply(a, b);
    }

    public static Vector3 Transform(this Vector3 vector, Transform3D transform)
    {
        return Transform3D.Transform(vector, transform);
    }

    public static Vector3 TransformNormal(this Vector3 normal, Transform3D transform)
    {
        return Transform3D.TransformNormal(normal, transform);
    }
}
