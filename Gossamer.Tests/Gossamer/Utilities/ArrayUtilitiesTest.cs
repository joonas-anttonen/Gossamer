using Gossamer.Utilities;

namespace Gossamer.Tests.Gossamer.Utilities;

[TestClass]
public class ArrayUtilitiesTest
{
    [TestMethod]
    public void ArrayUtilities_Write()
    {
        int[] array = new int[5];
        int[] expected = [1, 2, 3, 0, 0];
        int[] items = [1, 2, 3];

        int written = ArrayUtilities.Write(ref array, items);

        Assert.AreEqual(3, written);
        CollectionAssert.AreEqual(expected, array);

        array = new int[2];

        written = ArrayUtilities.Write(ref array, items);

        Assert.AreEqual(3, written);
        Assert.IsTrue(array.Length >= 3);
    }

    [TestMethod]
    public void ArrayUtilities_Reserve()
    {
        int[] array = new int[2];

        ArrayUtilities.Reserve(ref array, 5);

        Assert.AreEqual(10, array.Length);
    }

    [TestMethod]
    public void ArrayUtilities_Append()
    {
        int[] array = [1, 2, 3];
        int[] expected = [1, 2, 3, 4];

        ArrayUtilities.Append(ref array, 4);
        CollectionAssert.AreEqual(expected, array);

        array = [1, 2, 3];
        expected = [1, 2, 3, 4, 5];

        ArrayUtilities.Append(ref array, [4, 5]);
        CollectionAssert.AreEqual(expected, array);
    }

    [TestMethod]
    public void ArrayUtilities_Remove()
    {
        int[] array = [1, 2, 3];
        int[] expected = [1, 3];

        bool removed = ArrayUtilities.Remove(ref array, 2);

        Assert.IsTrue(removed);
        CollectionAssert.AreEqual(expected, array);

        array = [1, 2, 3];
        expected = [1, 2, 3];

        removed = ArrayUtilities.Remove(ref array, 4);

        Assert.IsFalse(removed);
        CollectionAssert.AreEqual(expected, array);

        array = [1];
        expected = [];

        removed = ArrayUtilities.Remove(ref array, 1);

        Assert.IsTrue(removed);
        CollectionAssert.AreEqual(expected, array);
    }
}