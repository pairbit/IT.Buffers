using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public static class BufferPool
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

    public static int TryReturn<T>(ReadOnlySequence<T> sequence, bool clear = true)
    {
        if (sequence.Start.GetObject() is SequenceSegment<T> segment)
        {
            var count = 0;
            do
            {
                var next = segment.Next;

                if (BufferPool<SequenceSegment<T>>.Shared.TryReturn(segment)) count++;

                segment = next!;

            } while (segment != null);

            if (clear)
            {
                Unsafe.As<ReadOnlySequence<T>, ReadOnlySequence<T>>(ref sequence) = ReadOnlySequence<T>.Empty;

                Debug.Assert(sequence.IsSingleSegment);
                Debug.Assert(sequence.IsEmpty);
                Debug.Assert(sequence.Start.GetObject() is T[] array && array.Length == 0);
            }

            return count;
        }

        return 0;
    }
}