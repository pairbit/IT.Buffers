using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers.Extensions;

public static class ArrayPoolShared
{
    public static void ReturnAndClear<T>(T[] array)
        => ArrayPool<T>.Shared.Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());

    public static bool TryReturnAndClear<T>(ArraySegment<T> arraySegment)
    {
        var array = arraySegment.Array;
        if (array != null && array.Length > 0)
        {
            ReturnAndClear(array);
            return true;
        }
        return false;
    }

    public static bool TryReturnAndClear<T>(ReadOnlyMemory<T> memory)
    {
        if (MemoryMarshal.TryGetArray(memory, out var arraySegment))
        {
            return TryReturnAndClear(arraySegment);
        }
        return false;
    }
}