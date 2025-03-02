namespace Gossamer;

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
