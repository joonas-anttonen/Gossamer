using System.Numerics;


namespace Gossamer.Tests.Gossamer;

[TestClass]
public class TransformTests
{
    [TestMethod]
    public void Constructor_WithTranslationAndRotation_ShouldInitializeCorrectly()
    {
        var translation = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);

        var transform = new Transform(translation, rotation);

        Assert.AreEqual(translation, transform.Translation);
        Assert.AreEqual(rotation, transform.Rotation);
    }

    [TestMethod]
    public void Constructor_WithTranslation_ShouldInitializeWithIdentityRotation()
    {
        var translation = new Vector3(1, 2, 3);

        var transform = new Transform(translation);

        Assert.AreEqual(translation, transform.Translation);
        Assert.AreEqual(Quaternion.Identity, transform.Rotation);
    }

    [TestMethod]
    public void ToMatrix_ShouldReturnCorrectMatrix()
    {
        var translation = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        var transform = new Transform(translation, rotation);

        var matrix = transform.ToMatrix();

        var expectedMatrix = Matrix4x4.CreateFromQuaternion(rotation);
        expectedMatrix.Translation = translation;

        Assert.AreEqual(expectedMatrix, matrix);
    }

    [TestMethod]
    public void Inverse_ShouldReturnCorrectInverseTransform()
    {
        var translation = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        var transform = new Transform(translation, rotation);

        var inverseTransform = transform.Inverse();

        var expectedTranslation = Vector3.Transform(-translation, Quaternion.Inverse(rotation));
        var expectedRotation = Quaternion.Inverse(rotation);

        Assert.AreEqual(expectedTranslation, inverseTransform.Translation);
        Assert.AreEqual(expectedRotation, inverseTransform.Rotation);
    }

    [TestMethod]
    public void Interpolate_ShouldReturnCorrectInterpolatedTransform()
    {
        var translationA = new Vector3(1, 2, 3);
        var rotationA = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        var transformA = new Transform(translationA, rotationA);

        var translationB = new Vector3(4, 5, 6);
        var rotationB = Quaternion.CreateFromYawPitchRoll(0.4f, 0.5f, 0.6f);
        var transformB = new Transform(translationB, rotationB);

        var t = 0.5f;
        var interpolatedTransform = Transform.Interpolate(transformA, transformB, t);

        var expectedTranslation = Vector3.Lerp(translationA, translationB, t);
        var expectedRotation = Quaternion.Slerp(rotationA, rotationB, t);

        Assert.AreEqual(expectedTranslation, interpolatedTransform.Translation);
        Assert.AreEqual(expectedRotation, interpolatedTransform.Rotation);
    }

    [TestMethod]
    public void Multiply_ShouldReturnCorrectMultipliedTransform()
    {
        var translationA = new Vector3(1, 2, 3);
        var rotationA = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        var transformA = new Transform(translationA, rotationA);

        var translationB = new Vector3(4, 5, 6);
        var rotationB = Quaternion.CreateFromYawPitchRoll(0.4f, 0.5f, 0.6f);
        var transformB = new Transform(translationB, rotationB);

        var multipliedTransform = Transform.Multiply(transformA, transformB);

        var expectedTranslation = Vector3.Transform(translationB, rotationA) + translationA;
        var expectedRotation = rotationA * rotationB;

        Assert.AreEqual(expectedTranslation, multipliedTransform.Translation);
        Assert.AreEqual(expectedRotation, multipliedTransform.Rotation);
    }

    [TestMethod]
    public void OperatorMultiply_ShouldReturnCorrectMultipliedTransform()
    {
        var translationA = new Vector3(1, 2, 3);
        var rotationA = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        var transformA = new Transform(translationA, rotationA);

        var translationB = new Vector3(4, 5, 6);
        var rotationB = Quaternion.CreateFromYawPitchRoll(0.4f, 0.5f, 0.6f);
        var transformB = new Transform(translationB, rotationB);

        var multipliedTransform = transformA * transformB;

        var expectedTranslation = Vector3.Transform(translationB, rotationA) + translationA;
        var expectedRotation = rotationA * rotationB;

        Assert.AreEqual(expectedTranslation, multipliedTransform.Translation);
        Assert.AreEqual(expectedRotation, multipliedTransform.Rotation);
    }

    [TestMethod]
    public void IdentityTransform_ShouldHaveZeroTranslationAndIdentityRotation()
    {
        var transform = new Transform(Vector3.Zero);

        Assert.AreEqual(Vector3.Zero, transform.Translation);
        Assert.AreEqual(Quaternion.Identity, transform.Rotation);
    }

    [TestMethod]
    public void TransformWithZeroTranslation_ShouldHaveCorrectRotation()
    {
        var rotation = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);
        var transform = new Transform(Vector3.Zero, rotation);

        Assert.AreEqual(Vector3.Zero, transform.Translation);
        Assert.AreEqual(rotation, transform.Rotation);
    }

    [TestMethod]
    public void TransformWithIdentityRotation_ShouldHaveCorrectTranslation()
    {
        var translation = new Vector3(1, 2, 3);
        var transform = new Transform(translation, Quaternion.Identity);

        Assert.AreEqual(translation, transform.Translation);
        Assert.AreEqual(Quaternion.Identity, transform.Rotation);
    }
}