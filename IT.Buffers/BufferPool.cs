using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public static class BufferPool
{
    public static RentedArray<T> RentArray<T>(int minimumLength)
    {
        var array = ArrayPool<T>.Shared.Rent(minimumLength);
        return new(array, 0, minimumLength, minimumLength == 0 || minimumLength > BufferSize.GB
            ? RentedArrayType.None : RentedArrayType.Shared);
    }

    public static RentedArray<T> RentArray<T>(int minimumLength, int maximumLength)
    {
        if (minimumLength == 0) return new([]);
        if (minimumLength > maximumLength)
        {
            return new(xArray.AllocateUninitialized<T>(minimumLength));
        }

        var array = ArrayPool<T>.Shared.Rent(minimumLength);
        return new(array, 0, minimumLength, RentedArrayType.Shared);
    }

    public static TBuffer Rent<TBuffer>() where TBuffer : class, IDisposable, new()
        => BufferPool<TBuffer>.Shared.Rent();

    public static void Return<T>(T[] array)
        => ArrayPool<T>.Shared.Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());

    public static bool TryReturn<T>(RentedArray<T> rentedArray)
    {
        var array = rentedArray.Array;
        if (array != null && array.Length > 0)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                array.Clear();
            }
            var type = rentedArray.Type;
            if (type == RentedArrayType.Shared)
            {
                ArrayPool<T>.Shared.Return(array);
                return true;
            }
            if (type != RentedArrayType.None)
                throw new InvalidOperationException($"the array is rented from {type} pool");
        }
        return false;
    }

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