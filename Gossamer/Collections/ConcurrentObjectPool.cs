using System.Collections.Concurrent;
using System.Diagnostics;

namespace Gossamer.Collections;

public class ConcurrentObjectPool<T> where T : class, new()
{
    readonly ConcurrentQueue<T> pool = new();

    public ConcurrentObjectPool(int initialCapacity)
    {
        for (var i = 0; i < initialCapacity; i++)
        {
            pool.Enqueue(new T());
        }
    }

    public T Rent()
    {
        if (pool.TryDequeue(out var item))
        {
            return item;
        }
        else
        {
            Debug.WriteLine("Object pool ran out of items. Creating a new one.");
            return new T();
        }
    }

    public void Return(T item)
    {
        pool.Enqueue(item);
    }
}