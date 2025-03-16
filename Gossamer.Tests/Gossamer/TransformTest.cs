namespace Gossamer.Tests.Gossamer;

[TestClass]
public class TransformTests
{
    [TestMethod]
    public void Transform_Apply()
    {
        Vector3 translation = new(1, 2, 3);
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        Transform3D transform = new(translation, rotation);

        Vector3 vector = new(4, 5, 6);
        Vector3 transformedVector = Transform3D.Transform(vector, transform);

        Vector3 expectedTransformedVector = Vector3.Transform(vector, rotation) + translation;

        Assert.AreEqual(expectedTransformedVector, transformedVector);
    }

    [TestMethod]
    public void Transform_ApplyNormal()
    {
        Vector3 translation = new(1, 2, 3);
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        Transform3D transform = new(translation, rotation);

        Vector3 normal = new(4, 5, 6);
        Vector3 transformedNormal = Transform3D.TransformNormal(normal, transform);

        Vector3 expectedTransformedNormal = Vector3.Transform(normal, rotation);

        Assert.AreEqual(expectedTransformedNormal, transformedNormal);
    }

    [TestMethod]
    public void Transform_Construct()
    {
        Vector3 translation = new(1, 2, 3);
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        Transform3D transform = new(translation, rotation);

        Assert.AreEqual(translation, transform.Translation);
        Assert.AreEqual(rotation, transform.Rotation);

        translation = new Vector3(1, 2, 3);
        transform = new Transform3D(translation);

        Assert.AreEqual(translation, transform.Translation);
        Assert.AreEqual(Quaternion.Identity, transform.Rotation);
    }

    [TestMethod]
    public void Transform_ToMatrix()
    {
        Vector3 translation = new(1, 2, 3);
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        Transform3D transform = new(translation, rotation);

        Matrix4x4 matrix = transform.ToMatrix();

        Matrix4x4 expectedMatrix = Matrix4x4.CreateFromQuaternion(rotation);
        expectedMatrix.Translation = translation;

        Assert.AreEqual(expectedMatrix, matrix);
    }

    [TestMethod]
    public void Transform_Inverse()
    {
        Vector3 translation = new(1, 2, 3);
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        Transform3D transform = new(translation, rotation);

        Transform3D inverseTransform = transform.Inverse();

        Vector3 expectedTranslation = Vector3.Transform(-translation, Quaternion.Inverse(rotation));
        Quaternion expectedRotation = Quaternion.Inverse(rotation);

        Assert.AreEqual(expectedTranslation, inverseTransform.Translation);
        Assert.AreEqual(expectedRotation, inverseTransform.Rotation);
    }

    [TestMethod]
    public void Transform_Interpolate()
    {
        Vector3 translationA = new(1, 2, 3);
        Quaternion rotationA = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        Transform3D transformA = new(translationA, rotationA);

        Vector3 translationB = new(4, 5, 6);
        Quaternion rotationB = Quaternion.CreateFromYawPitchRoll(0.4f, 0.5f, 0.6f);
        Transform3D transformB = new(translationB, rotationB);

        float t = 0.5f;
        Transform3D interpolatedTransform = Transform3D.Interpolate(transformA, transformB, t);

        Vector3 expectedTranslation = Vector3.Lerp(translationA, translationB, t);
        Quaternion expectedRotation = Quaternion.Slerp(rotationA, rotationB, t);

        Assert.AreEqual(expectedTranslation, interpolatedTransform.Translation);
        Assert.AreEqual(expectedRotation, interpolatedTransform.Rotation);
    }

    [TestMethod]
    public void Transform_Multiply()
    {
        Vector3 translationA = new(1, 2, 3);
        Quaternion rotationA = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        Transform3D transformA = new(translationA, rotationA);

        Vector3 translationB = new(4, 5, 6);
        Quaternion rotationB = Quaternion.CreateFromYawPitchRoll(0.4f, 0.5f, 0.6f);
        Transform3D transformB = new(translationB, rotationB);

        Transform3D multipliedTransform = Transform3D.Multiply(transformA, transformB);

        Vector3 expectedTranslation = Vector3.Transform(translationB, rotationA) + translationA;
        Quaternion expectedRotation = rotationA * rotationB;

        Assert.AreEqual(expectedTranslation, multipliedTransform.Translation);
        Assert.AreEqual(expectedRotation, multipliedTransform.Rotation);
    }

    [TestMethod]
    public void Transform_OperatorMultiply()
    {
        Vector3 translationA = new(1, 2, 3);
        Quaternion rotationA = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        Transform3D transformA = new(translationA, rotationA);

        Vector3 translationB = new(4, 5, 6);
        Quaternion rotationB = Quaternion.CreateFromYawPitchRoll(0.4f, 0.5f, 0.6f);
        Transform3D transformB = new(translationB, rotationB);

        Transform3D multipliedTransform = transformA * transformB;

        Vector3 expectedTranslation = Vector3.Transform(translationB, rotationA) + translationA;
        Quaternion expectedRotation = rotationA * rotationB;

        Assert.AreEqual(expectedTranslation, multipliedTransform.Translation);
        Assert.AreEqual(expectedRotation, multipliedTransform.Rotation);
    }

    [TestMethod]
    public void Transform_Identity()
    {
        Transform3D transform = new(Vector3.Zero);

        Assert.AreEqual(Vector3.Zero, transform.Translation);
        Assert.AreEqual(Quaternion.Identity, transform.Rotation);
    }

    [TestMethod]
    public void Transform_IdentityTranslation()
    {
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        Transform3D transform = new(Vector3.Zero, rotation);

        Assert.AreEqual(Vector3.Zero, transform.Translation);
        Assert.AreEqual(rotation, transform.Rotation);
    }

    [TestMethod]
    public void Transform_IdentityRotation()
    {
        Vector3 translation = new(1, 2, 3);
        Transform3D transform = new(translation, Quaternion.Identity);

        Assert.AreEqual(translation, transform.Translation);
        Assert.AreEqual(Quaternion.Identity, transform.Rotation);
    }
}