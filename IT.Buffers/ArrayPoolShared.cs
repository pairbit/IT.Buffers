using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers;

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

    public static bool TryReturnAndClear<T>(Memory<T> memory)
        => TryReturnAndClear((ReadOnlyMemory<T>)memory);

    public static int TryReturnAndClear<T>(ReadOnlySequence<T> sequence)
    {
        if (sequence.IsSingleSegment) return TryReturnAndClear(sequence.First) ? 1 : 0;

        if (sequence.Start.GetObject() is SequenceSegment<T> segment)
        {
            var count = 0;
            do
            {
                var next = segment.GetNext();

                SequenceSegment<T>.Pool.Return(segment);

                segment = next!;

                count++;
            } while (segment != null);

            return count;
        }

        return 0;
    }
}