using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public static class ArrayPoolShared
{
    public static void Return<T>(T[] array)
        => ArrayPool<T>.Shared.Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());

    public static bool TryReturn<T>(ArraySegment<T> arraySegment)
    {
        var array = arraySegment.Array;
        if (array != null && array.Length > 0)
        {
            Return(array);
            return true;
        }
        return false;
    }

    public static bool TryReturn<T>(ReadOnlyMemory<T> memory)
    {
        if (MemoryMarshal.TryGetArray(memory, out var arraySegment))
        {
            return TryReturn(arraySegment);
        }
        return false;
    }

    public static bool TryReturn<T>(Memory<T> memory)
        => TryReturn((ReadOnlyMemory<T>)memory);

    public static int TryReturn<T>(ReadOnlySequence<T> sequence)
    {
        if (sequence.IsSingleSegment) return TryReturn(sequence.First) ? 1 : 0;

        if (sequence.Start.GetObject() is SequenceSegment<T> segment)
        {
            var count = 0;
            do
            {
                var next = segment.Next;

                SequenceSegment<T>.Pool.Return(segment);

                segment = next!;

                count++;
            } while (segment != null);

            return count;
        }

        return 0;
    }
}