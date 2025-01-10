#if NETSTANDARD2_1

using IT.Buffers;

namespace System.Collections.Generic;

internal static class xList
{
    private const int DefaultCapacity = 4;

    public static int EnsureCapacity<T>(this List<T> list, int capacity)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        var length = list.Capacity;
        if (length < capacity)
        {
            return list.Grow(length, capacity);
        }

        return length;
    }

    private static int Grow<T>(this List<T> list, int length, int capacity)
    {
        int newcapacity = length == 0 ? DefaultCapacity : 2 * length;

        // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint)newcapacity > BufferSize.Max) newcapacity = BufferSize.Max;

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < capacity) newcapacity = capacity;

        list.Capacity = newcapacity;

        return newcapacity;
    }
}

#endif