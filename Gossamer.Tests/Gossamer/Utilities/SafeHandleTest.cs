using System.Text;

using Gossamer.Utilities;

namespace Gossamer.Tests.Gossamer.Utilities;

[TestClass]
public class SafeHandleTests
{
    [TestMethod]
    public void SafeNativeBlob_AllocatesAndFreesMemory()
    {
        byte[] data = { 1, 2, 3, 4, 5 };
        SafeNativeBlob blob = new(data);

        Assert.IsFalse(blob.IsInvalid);
        Assert.AreEqual(data.Length, blob.Size);

        blob.Dispose();
        Assert.IsTrue(blob.IsInvalid);
    }

    [TestMethod]
    public void SafeNativeString_AllocatesAndFreesMemory()
    {
        string testString = "Hello, World!";
        SafeNativeString safeString = new(testString);

        Assert.IsFalse(safeString.IsInvalid);
        Assert.AreEqual(Encoding.UTF8, safeString.Encoding);

        safeString.Dispose();
        Assert.IsTrue(safeString.IsInvalid);
    }

    [TestMethod]
    public void SafeNativeStringArray_AllocatesAndFreesMemory()
    {
        int capacity = 3;
        SafeNativeStringArray stringArray = new(capacity);

        Assert.IsFalse(stringArray.IsInvalid);
        Assert.AreEqual(capacity, stringArray.Capacity);
        Assert.AreEqual(0, stringArray.Count);

        stringArray.Dispose();
        Assert.IsTrue(stringArray.IsInvalid);
    }

    [TestMethod]
    public void SafeNativeStringArray_AddStrings()
    {
        int capacity = 2;
        SafeNativeStringArray stringArray = new(capacity);

        stringArray.Add("First");
        stringArray.Add("Second");

        Assert.AreEqual(2, stringArray.Count);

        Assert.ThrowsException<InvalidOperationException>(() => stringArray.Add("Third"));

        stringArray.Dispose();
    }
}