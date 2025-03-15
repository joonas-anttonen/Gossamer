using Gossamer.Utilities;

namespace Gossamer.Tests.Gossamer.Utilities;

[TestClass]
public class ArrayUtilitiesTest
{
    [TestMethod]
    public void Write_ShouldWriteItemsToArray()
    {
        int[] array = new int[5];
        var items = new List<int> { 1, 2, 3 };

        int written = ArrayUtilities.Write(ref array, items);

        Assert.AreEqual(3, written);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 0, 0 }, array);
    }

    [TestMethod]
    public void Write_ShouldResizeArrayIfNeeded()
    {
        int[] array = new int[2];
        var items = new List<int> { 1, 2, 3 };

        int written = ArrayUtilities.Write(ref array, items);

        Assert.AreEqual(3, written);
        Assert.IsTrue(array.Length >= 3);
    }

    [TestMethod]
    public void Reserve_ShouldIncreaseArraySize()
    {
        int[] array = new int[2];

        ArrayUtilities.Reserve(ref array, 5);

        Assert.AreEqual(10, array.Length);
    }

    [TestMethod]
    public void Append_ShouldAddElementToArray()
    {
        int[] array = [1, 2, 3];

        ArrayUtilities.Append(ref array, 4);

        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, array);
    }

    [TestMethod]
    public void Append_ShouldAddElementsToArray()
    {
        int[] array = [1, 2, 3];
        int[] children = [4, 5];

        ArrayUtilities.Append(ref array, children);

        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5 }, array);
    }

    [TestMethod]
    public void Remove_ShouldRemoveElementFromArray()
    {
        int[] array = [1, 2, 3];

        bool removed = ArrayUtilities.Remove(ref array, 2);

        Assert.IsTrue(removed);
        CollectionAssert.AreEqual(new int[] { 1, 3 }, array);
    }

    [TestMethod]
    public void Remove_ShouldReturnFalseIfElementNotFound()
    {
        int[] array = [1, 2, 3];

        bool removed = ArrayUtilities.Remove(ref array, 4);

        Assert.IsFalse(removed);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, array);
    }

    [TestMethod]
    public void Remove_ShouldReturnEmptyArrayIfLastElementRemoved()
    {
        int[] array = [1];

        bool removed = ArrayUtilities.Remove(ref array, 1);

        Assert.IsTrue(removed);
        CollectionAssert.AreEqual(new int[] { }, array);
    }
}