using System.Collections;

using Gossamer.Utilities;

namespace Gossamer.Collections;

/// <summary>
/// A simple ring buffer implementation.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="capacity"> The capacity of the ring buffer. </param>
public class RingBuffer<T>(int capacity) : IEnumerable<T> where T : INumber<T>
{
    readonly T[] buffer = new T[capacity < 1 ? throw new ArgumentOutOfRangeException(nameof(capacity)) : capacity];
    int head;

    /// <summary>
    /// The maximum number of elements the ring buffer can hold.
    /// </summary>
    public int Capacity => buffer.Length;

    /// <summary>
    /// The current number of elements in the ring buffer.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index"></param>
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
        Array.Clear(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// Pushes an item onto the ring buffer.
    /// </summary>
    /// <param name="item"> The item to push. </param>
    public void Push(T item)
    {
        buffer[head] = item;
        head = (head + 1) % buffer.Length;
        Count = Math.Min(Count + 1, buffer.Length);
    }

    /// <summary>
    /// Calculates the minimum, maximum, and average values in the ring buffer.
    /// </summary>
    /// <param name="min"> The minimum. </param>
    /// <param name="max"> The maximum. </param>
    /// <param name="mean"> The mean. </param>
    public void CalculateMinMaxMean(out T min, out T max, out T mean)
    {
        min = max = mean = buffer[0];

        if (Count == 0)
        {
            return;
        }

        for (int i = 1; i < Count; i++)
        {
            var value = buffer[i];

            if (value < min)
            {
                min = value;
            }
            else if (value > max)
            {
                max = value;
            }

            mean += value;
        }

        mean /= T.CreateChecked(Count);
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