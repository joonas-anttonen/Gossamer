using System.Collections;

using Gossamer.Collections;

namespace Gossamer.Tests.Gossamer.Collections;

[TestClass]
public class RingBufferTest
{
    [TestMethod]
    public void TestPushAndCount()
    {
        var ringBuffer = new RingBuffer<int>(3);
        ringBuffer.Push(1);
        ringBuffer.Push(2);
        ringBuffer.Push(3);

        Assert.AreEqual(3, ringBuffer.Count);
    }

    [TestMethod]
    public void TestPushOverCapacity()
    {
        var ringBuffer = new RingBuffer<int>(3);
        ringBuffer.Push(1);
        ringBuffer.Push(2);
        ringBuffer.Push(3);
        ringBuffer.Push(4);

        Assert.AreEqual(3, ringBuffer.Count);
        Assert.AreEqual(2, ringBuffer[0]);
        Assert.AreEqual(3, ringBuffer[1]);
        Assert.AreEqual(4, ringBuffer[2]);
    }

    [TestMethod]
    public void TestClear()
    {
        var ringBuffer = new RingBuffer<int>(3);
        ringBuffer.Push(1);
        ringBuffer.Push(2);
        ringBuffer.Push(3);
        ringBuffer.Clear();

        Assert.AreEqual(0, ringBuffer.Count);
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
    private static readonly int[] expected = [1, 2, 3];

    [TestMethod]
    public void TestEnumeration()
    {
        var ringBuffer = new RingBuffer<int>(3);
        ringBuffer.Push(1);
        ringBuffer.Push(2);
        ringBuffer.Push(3);

        var items = ringBuffer.ToArray();
        CollectionAssert.AreEqual(expected, items);

        var plainEnumerable = ((IEnumerable)ringBuffer).GetEnumerator();
        var genericEnumerable = ((IEnumerable<int>)ringBuffer).GetEnumerator();

        for (int i = 0; i < 3; i++)
        {
            Assert.IsTrue(plainEnumerable.MoveNext());
            Assert.IsTrue(genericEnumerable.MoveNext());
            Assert.AreEqual(expected[i], plainEnumerable.Current);
            Assert.AreEqual(expected[i], genericEnumerable.Current);
        }

    }
}