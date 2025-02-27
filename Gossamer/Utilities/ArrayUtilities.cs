namespace Gossamer.Utilities;

public static class ArrayUtilities
{
    public static int Write<T>(ref T[] array, IEnumerable<T> items, int startIndex = 0)
    {
        int written = 0;
        int writeIndex = startIndex;
        foreach (var item in items)
        {
            if (writeIndex >= array.Length)
            {
                Reserve(ref array, writeIndex + 1);
            }
            array[writeIndex++] = item;
            written++;
        }
        return written;
    }

    /// <summary>
    /// Reserves the specified amount of space in the array. 
    /// <para>If the array is already larger than the specified amount, nothing happens.</para>
    /// <para>If the array is smaller, a new array (that is <paramref name="growthFactor"/> times larger) is created and the old array is copied into it.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <param name="amount"></param>
    /// <param name="growthFactor"></param>
    public static void Reserve<T>(ref T[] arr, int amount, int growthFactor = 2)
    {
        if (arr.Length < amount)
        {
            T[] newArr = new T[amount * growthFactor];
            arr.CopyTo(newArr, 0);
            arr = newArr;
        }
    }

    public static void Append<T>(ref T[] array, T child)
    {
        Array.Resize(ref array, array.Length + 1);
        array[^1] = child;
    }

    public static void Append<T>(ref T[] array, T[] children)
    {
        Array.Resize(ref array, array.Length + children.Length);
        children.CopyTo(array, array.Length - children.Length);
    }

    /// <summary>
    /// Removes the specified element from the array, if it exists. Resizes the array if the element is removed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="element"></param>
    /// <returns> <see langword="true"/> if the element was removed, <see langword="false"/> otherwise. </returns>
    public static bool Remove<T>(ref T[] array, T element)
    {
        int index = Array.IndexOf(array, element);
        if (index < 0)
        {
            return false;
        }

        if (array.Length == 1)
        {
            array = [];
        }
        else
        {
            var newArray = new T[array.Length - 1];
            for (int i = 0, io = 0; i < array.Length; i++)
            {
                if (i < index)
                {
                    newArray[io++] = array[i];
                }
                else if (i > index)
                {
                    newArray[io++] = array[i];
                }
            }
            array = newArray;
        }

        return true;
    }
}