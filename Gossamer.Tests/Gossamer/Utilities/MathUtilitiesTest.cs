using Gossamer.Utilities;

namespace Gossamer.Tests.Gossamer.Utilities;

[TestClass]
public class MathUtilitiesTest
{
    [TestMethod]
    public void MathUtilities_DotProduct()
    {
        Vector3 a = new(1, 2, 3);
        Vector3 b = new(4, 5, 6);
        float result = MathUtilities.Dot(a, b);
        Assert.AreEqual(32, result);
    }

    [TestMethod]
    public void MathUtilities_CrossProduct()
    {
        Vector3 a = new(1, 2, 3);
        Vector3 b = new(4, 5, 6);
        Vector3 result = MathUtilities.Cross(a, b);
        Assert.AreEqual(new Vector3(-3, 6, -3), result);
    }

    [TestMethod]
    public void MathUtilities_Normalize()
    {
        Vector3 v = new(1, 2, 3);
        Vector3 result = MathUtilities.Normalize(v);
        Assert.AreEqual(new Vector3(0.26726124f, 0.5345225f, 0.8017837f), result);
    }

    [TestMethod]
    public void MathUtilities_Transform()
    {
        Vector3 v = new(1, 2, 3);
        Matrix4x4 m = Matrix4x4.Identity;
        Vector3 result = MathUtilities.Transform(v, m);
        Assert.AreEqual(v, result);
    }

    [TestMethod]
    public void MathUtilities_TransformNormal()
    {
        Vector3 v = new(1, 2, 3);
        Matrix4x4 m = Matrix4x4.Identity;
        Vector3 result = MathUtilities.TransformNormal(v, m);
        Assert.AreEqual(v, result);
    }

    [TestMethod]
    public void MathUtilities_AngleBetweenVectors2D()
    {
        Vector2 a = new(1, 0);
        Vector2 b = new(0, 1);
        float result = MathUtilities.Angle(a, b);
        Assert.AreEqual(MathF.PI / 2, result);
    }

    [TestMethod]
    public void MathUtilities_AngleBetweenVectors3D()
    {
        Vector3 a = new(1, 0, 0);
        Vector3 b = new(0, 1, 0);
        float result = MathUtilities.Angle(a, b);
        Assert.AreEqual(MathF.PI / 2, result);
    }

    [TestMethod]
    public void MathUtilities_ReciprocalVector2()
    {
        Vector2 v = new(2, 4);
        Vector2 result = MathUtilities.Reciprocal(v);
        Assert.AreEqual(new Vector2(0.5f, 0.25f), result);
    }

    [TestMethod]
    public void MathUtilities_ReciprocalVector3()
    {
        Vector3 v = new(2, 4, 8);
        Vector3 result = MathUtilities.Reciprocal(v);
        Assert.AreEqual(new Vector3(0.5f, 0.25f, 0.125f), result);
    }

    [TestMethod]
    public void MathUtilities_InvertMatrix()
    {
        Matrix4x4 m = Matrix4x4.Identity;
        Matrix4x4 result = MathUtilities.Invert(m);
        Assert.AreEqual(m, result);
    }

    [TestMethod]
    public void MathUtilities_AlmostEqualVector3()
    {
        Vector3 a = new(1, 2, 3);
        Vector3 b = new(1.000001f, 2.000001f, 3.000001f);
        bool result = MathUtilities.AlmostEqual(a, b);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void MathUtilities_AlmostEqualVector2()
    {
        Vector2 a = new(1, 2);
        Vector2 b = new(1.000001f, 2.000001f);
        bool result = MathUtilities.AlmostEqual(a, b);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void MathUtilities_AlmostEqualFloat()
    {
        float a = 1.000001f;
        float b = 1.000002f;
        float epsilon = 0.00001f;
        bool result = MathUtilities.AlmostEqual(a, b, epsilon);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void MathUtilities_Frac()
    {
        float v = 1.5f;
        float result = MathUtilities.Frac(v);
        Assert.AreEqual(0.5f, result);
    }

    [TestMethod]
    public void MathUtilities_Align()
    {
        uint value = 5;
        uint alignment = 4;
        uint result = MathUtilities.Align(value, alignment);
        Assert.AreEqual(2u, result);
    }

    [TestMethod]
    public void MathUtilities_Radians()
    {
        float degrees = 180;
        float result = MathUtilities.Radians(degrees);
        Assert.AreEqual(MathF.PI, result);
    }

    [TestMethod]
    public void MathUtilities_Degrees()
    {
        float radians = MathF.PI;
        float result = MathUtilities.Degrees(radians);
        Assert.AreEqual(180, result);
    }

    [TestMethod]
    public void MathUtilities_ClampInt()
    {
        int value = 5;
        int min = 1;
        int max = 10;
        int result = MathUtilities.Clamp(value, min, max);
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void MathUtilities_ClampFloat()
    {
        float value = 5.5f;
        float min = 1.0f;
        float max = 10.0f;
        float result = MathUtilities.Clamp(value, min, max);
        Assert.AreEqual(5.5f, result);
    }

    [TestMethod]
    public void MathUtilities_WrapInt()
    {
        int value = 12;
        int min = 0;
        int max = 10;
        int result = MathUtilities.Wrap(value, min, max);
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void MathUtilities_WrapIntWithNegativeValue()
    {
        int value = -2;
        int min = 0;
        int max = 10;
        int result = MathUtilities.Wrap(value, min, max);
        Assert.AreEqual(8, result);
    }

    [TestMethod]
    public void MathUtilities_WrapIntWithInvalidRange()
    {
        int value = 12;
        int min = 10;
        int max = 0;
        Assert.ThrowsException<ArgumentException>(() => MathUtilities.Wrap(value, min, max));
    }

    [TestMethod]
    public void MathUtilities_WrapFloat()
    {
        float value = 12.5f;
        float min = 0.0f;
        float max = 10.0f;
        float result = MathUtilities.Wrap(value, min, max);
        Assert.AreEqual(2.5f, result);
    }

    [TestMethod]
    public void MathUtilities_WrapFloatWithInverseRange()
    {
        float value = 2.5f;
        float min = 10.0f;
        float max = 0.0f;
        Assert.ThrowsException<ArgumentException>(() => MathUtilities.Wrap(value, min, max));
    }
}