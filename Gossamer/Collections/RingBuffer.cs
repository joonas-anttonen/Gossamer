using System.Collections;

using Gossamer.Utilities;

namespace Gossamer.Collections;

/// <summary>
/// A simple ring buffer implementation.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="capacity">The capacity of the ring buffer.</param>
public class RingBuffer<T>(int capacity) : IEnumerable<T>
{
    readonly T[] buffer = new T[capacity];
    int head;

    /// <summary>
    /// The number of elements in the ring buffer.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T this[int index]
    {
        get => buffer[MathUtilities.Wrap(head + index, 0, buffer.Length)];      
    }

    /// <summary>
    /// Clears the ring buffer.
    /// </summary>
    public void Clear()
    {
        head = 0;
        Count = 0;
    }

    /// <summary>
    /// Pushes an item onto the ring buffer.
    /// </summary>
    /// <param name="item"></param>
    public void Push(T item)
    {
        buffer[head] = item;
        head = (head + 1) % buffer.Length;
        Count = Math.Min(Count + 1, buffer.Length);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }
}