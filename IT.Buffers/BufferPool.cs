using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public static class BufferPool
{
    public static MemoryPool<T> CreateMemoryPool<T>(ArrayPool<T> pool, bool clearArray, int defaultBufferSize,
        int maxBufferSize = BufferSize.Max) =>
        new ConfigurableMemoryPool<T>(pool, defaultBufferSize, maxBufferSize, clearArray);

    public static MemoryPool<T> CreateMemoryPool<T>(ArrayPool<T> pool, int maxBufferSize = BufferSize.Max) =>
        new ConfigurableMemoryPool<T>(pool, BufferSize<T>.KB_4, maxBufferSize, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());

    public static Buffer<T> Rent<T>(int minimumLength)
    {
        var array = ArrayPool<T>.Shared.Rent(minimumLength);
        return new(array, 0, minimumLength, minimumLength == 0 || minimumLength > BufferSize.GB
            ? RentedArrayType.None : RentedArrayType.Shared);
    }

    public static Buffer<T> Rent<T>(int minimumLength, int maximumLength)
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

    public static bool TryReturn<T>(Buffer<T> buffer)
    {
        var memoryOwner = buffer.MemoryOwner;
        if (memoryOwner != null)
        {
            memoryOwner.Dispose();
            return true;
        }

        var array = buffer.Array;
        if (array != null && array.Length > 0)
        {
            var arrayType = buffer.ArrayType;
            if (arrayType == RentedArrayType.Shared)
            {
                Return(array);
                return true;
            }
            if (arrayType != RentedArrayType.None)
                throw new InvalidOperationException($"the array is rented from {arrayType} pool");
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
        if (sequence.Start.GetObject() is RentableSequenceSegment<T> segment)
            return TryReturnSegments(segment);

        return 0;
    }

    public static int TryReturnSegments<T>(RentableSequenceSegment<T> segment)
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