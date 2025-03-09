using System.Collections;

using Gossamer.Collections;

namespace Gossamer.Tests.Gossamer.Collections;

[TestClass]
public class RingBufferTest
{
    const int TestCapacity = 3;

    [TestMethod]
    public void TestConstructor()
    {
        var ringBuffer = new RingBuffer<int>(TestCapacity);
        Assert.AreEqual(TestCapacity, ringBuffer.Capacity);
    }

    [TestMethod]
    public void TestConstructorOutOfRangeCapacity()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
    }

    [TestMethod]
    public void TestPushAndCount()
    {
        // Arrange
        var ringBuffer = new RingBuffer<int>(TestCapacity);

        // Act
        for (int i = 0; i < TestCapacity; i++)
        {
            ringBuffer.Push(i);
        }

        // Assert
        Assert.AreEqual(TestCapacity, ringBuffer.Count);

        for (int i = 0; i < TestCapacity; i++)
        {
            Assert.AreEqual(i, ringBuffer[i]);
        }
    }

    [TestMethod]
    public void TestPushOverCapacity()
    {
        // Arrange
        var ringBuffer = new RingBuffer<int>(TestCapacity);

        // Act
        for (int i = 0; i < TestCapacity + 1; i++)
        {
            ringBuffer.Push(i);
        }

        // Assert
        Assert.AreEqual(3, ringBuffer.Count);
        Assert.AreEqual(1, ringBuffer[0]);
        Assert.AreEqual(2, ringBuffer[1]);
        Assert.AreEqual(3, ringBuffer[2]);
    }

    [TestMethod]
    public void TestClear()
    {
        // Arrange
        var ringBuffer = new RingBuffer<int>(TestCapacity);
        for (int i = 0; i < TestCapacity; i++)
        {
            ringBuffer.Push(i);
        }

        // Act
        ringBuffer.Clear();

        // Assert
        Assert.AreEqual(0, ringBuffer.Count);

        for (int i = 0; i < TestCapacity; i++)
        {
            Assert.AreEqual(0, ringBuffer[i]);
        }
    }

    [TestMethod]
    public void TestIndexer()
    {
        var ringBuffer = new RingBuffer<int>(3);
        ringBuffer.Push(1);
        ringBuffer.Push(2);
        ringBuffer.Push(3);

        Assert.AreEqual(1, ringBuffer[0]);
        Assert.AreEqual(2, ringBuffer[1]);
        Assert.AreEqual(3, ringBuffer[2]);
    }

    [TestMethod]
    public void TestCalculateMinMaxMean()
    {
        var ringBuffer = new RingBuffer<int>(3);
        ringBuffer.Push(1);
        ringBuffer.Push(2);
        ringBuffer.Push(3);

        ringBuffer.CalculateMinMaxMean(out var min, out var max, out var average);

        Assert.AreEqual(1, min);
        Assert.AreEqual(3, max);
        Assert.AreEqual(2, average);

        ringBuffer.Clear();
        ringBuffer.CalculateMinMaxMean(out min, out max, out average);

        Assert.AreEqual(0, min);
        Assert.AreEqual(0, max);
        Assert.AreEqual(0, average);

        ringBuffer.Push(-1);
        ringBuffer.Push(-2);
        ringBuffer.Push(-3);

        ringBuffer.CalculateMinMaxMean(out min, out max, out average);

        Assert.AreEqual(-3, min);
        Assert.AreEqual(-1, max);
        Assert.AreEqual(-2, average);
    }

    static readonly int[] enumerationExpected = [1, 2, 3];

    [TestMethod]
    public void TestEnumeration()
    {
        var ringBuffer = new RingBuffer<int>(3);
        ringBuffer.Push(1);
        ringBuffer.Push(2);
        ringBuffer.Push(3);

        var items = ringBuffer.ToArray();
        CollectionAssert.AreEqual(enumerationExpected, items);

        var plainEnumerable = ((IEnumerable)ringBuffer).GetEnumerator();
        var genericEnumerable = ((IEnumerable<int>)ringBuffer).GetEnumerator();

        for (int i = 0; i < 3; i++)
        {
            Assert.IsTrue(plainEnumerable.MoveNext());
            Assert.IsTrue(genericEnumerable.MoveNext());
            Assert.AreEqual(enumerationExpected[i], plainEnumerable.Current);
            Assert.AreEqual(enumerationExpected[i], genericEnumerable.Current);
        }

    }
}