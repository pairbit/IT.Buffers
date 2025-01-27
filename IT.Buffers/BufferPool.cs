using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public static class BufferPool
{
    public static TBuffer Rent<TBuffer>() where TBuffer : class, IDisposable, new()
        => BufferPool<TBuffer>.Shared.Rent();

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

    public static int TryReturn<T>(in ReadOnlySequence<T> sequence)
    {
        if (sequence.Start.GetObject() is SequenceSegment<T> segment)
            return TryReturnSegments(segment);

        return 0;
    }

    public static int TryReturnSegments<T>(SequenceSegment<T> segment)
    {
        var count = 0;
        do
        {
            var next = segment.Next;

            if (TryReturn(segment)) count++;

            segment = next!;

        } while (segment != null);

        return count;
    }

    public static bool TryReturn<TBuffer>(TBuffer buffer) where TBuffer : class, IDisposable, new()
        => BufferPool<TBuffer>.Shared.TryReturn(buffer);
}