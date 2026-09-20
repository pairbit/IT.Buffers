using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
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
        return new(array, 0, minimumLength, minimumLength > BufferSize.GB
            ? RentedArrayType.None : RentedArrayType.Shared);
    }

    public static TBuffer Rent<TBuffer>() where TBuffer : class, IResetable, new()
        => NewBufferPool<TBuffer>.Shared.Rent();

    public static bool TryRent<TBuffer>([MaybeNullWhen(false)] out TBuffer buffer) where TBuffer : class, IResetable, new()
        => NewBufferPool<TBuffer>.Shared.TryRent(out buffer);

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
        if (sequence.Start.GetObject() is ReadOnlySequenceSegment<T> segment)
            return TryReturnSegments(segment);

        return 0;
    }

    public static bool TryReturn<TBuffer>(TBuffer buffer) where TBuffer : class, IResetable, new()
        => NewBufferPool<TBuffer>.Shared.TryReturn(buffer);

    public static void Return<TBuffer>(TBuffer buffer) where TBuffer : class, IResetable, new()
        => NewBufferPool<TBuffer>.Shared.Return(buffer);

    internal static int TryReturnSegments<T>(ReadOnlySequenceSegment<T> segment)
    {
        var count = 0;
        do
        {
            var next = segment.Next;

            if (segment is IDisposable disposable)
            {
                disposable.Dispose();
                count++;
            }
            else if (segment is SharedSequenceSegment<T> sharedSequenceSegment)
            {
                SharedSequenceSegment<T>.Pool.Return(sharedSequenceSegment);
                count++;
            }

            segment = next!;

        } while (segment != null);

        return count;
    }
}