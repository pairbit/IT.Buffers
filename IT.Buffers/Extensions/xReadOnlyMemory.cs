using IT.Buffers.Interfaces;
using System;
using System.Buffers;

namespace IT.Buffers.Extensions;

public static class xReadOnlyMemory
{
    public static ReadOnlySequence<T> ToSequenceBySegments<T>(this Memory<T> memory, int maxSegments)
        => ToSequenceBySegments((ReadOnlyMemory<T>)memory, maxSegments);

    public static ReadOnlySequence<T> ToSequenceBySegments<T>(this ReadOnlyMemory<T> memory, int maxSegments)
    {
        if (maxSegments <= 0) throw new ArgumentOutOfRangeException(nameof(maxSegments));

        var length = memory.Length;
        if (length == 0) return ReadOnlySequence<T>.Empty;

        var segments = length < maxSegments ? length : maxSegments;
        if (segments == 1) return new(memory);

        var segmentLength = length / segments;

        var start = new RentableSequenceSegment<T>
        {
            Memory = memory[..segmentLength]
        };

        memory = memory[segmentLength..];
        var end = start;

        for (int i = segments - 2; i > 0; i--)
        {
            end = end.Append(memory[..segmentLength]);

            memory = memory[segmentLength..];
        }

        end = end.Append(memory);
        return new ReadOnlySequence<T>(start, 0, end, end.Memory.Length);
    }

    public static ReadOnlySequence<T> ToSequence<T>(this Memory<T> memory,
        int bufferSize = 0, IBufferGrowthStrategy? growthStrategy = null)
        => ToSequence((ReadOnlyMemory<T>)memory, bufferSize, growthStrategy);

    public static ReadOnlySequence<T> ToSequenceRented<T>(this Memory<T> memory,
        int bufferSize = 0, IBufferGrowthStrategy? growthStrategy = null,
        bool isRented = false)
        => ToSequenceRented((ReadOnlyMemory<T>)memory, bufferSize, growthStrategy, isRented);

    public static ReadOnlySequence<T> ToSequence<T>(this ReadOnlyMemory<T> memory,
        int bufferSize = 0, IBufferGrowthStrategy? growthStrategy = null)
    {
        if (memory.IsEmpty) return ReadOnlySequence<T>.Empty;

        if (bufferSize < 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        if (growthStrategy == null)
            growthStrategy = BufferGrowthStrategy.OneOfEachSize;

        if (bufferSize == 0)
            bufferSize = growthStrategy.GetBufferSize<T>();

        if (bufferSize >= memory.Length) return new ReadOnlySequence<T>(memory);

        var start = new RentableSequenceSegment<T>
        {
            Memory = memory[..bufferSize]
        };

        memory = memory[bufferSize..];
        var end = start;

        do
        {
            bufferSize = growthStrategy.Grow(bufferSize);

            if (memory.Length < bufferSize) bufferSize = memory.Length;

            end = end.Append(memory[..bufferSize]);

            memory = memory[bufferSize..];
        } while (memory.Length > 0);

        return new ReadOnlySequence<T>(start, 0, end, end.Memory.Length);
    }

    public static ReadOnlySequence<T> ToSequenceRented<T>(this ReadOnlyMemory<T> memory,
        int bufferSize = 0, IBufferGrowthStrategy? growthStrategy = null,
        bool isRented = false)
    {
        if (memory.IsEmpty) return ReadOnlySequence<T>.Empty;

        if (bufferSize < 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        if (growthStrategy == null)
            growthStrategy = BufferGrowthStrategy.OneOfEachSize;

        if (bufferSize == 0)
            bufferSize = growthStrategy.GetBufferSize<T>();

        if (bufferSize >= memory.Length)
        {
            //If the memory is rented, then we expect to wrap the segment,
            //otherwise the memory will not be returned to the pool
            if (isRented)
            {
                var single = BufferPool<RentableSequenceSegment<T>>.Shared.Rent();
                single.SetMemory(memory, isRented: true);
                return new ReadOnlySequence<T>(single, 0, single, memory.Length);
            }

            return new ReadOnlySequence<T>(memory);
        }

        var start = BufferPool<RentableSequenceSegment<T>>.Shared.Rent();
        start.SetMemory(memory[..bufferSize], isRented);

        memory = memory[bufferSize..];
        var end = start;

        do
        {
            bufferSize = growthStrategy.Grow(bufferSize);

            if (memory.Length < bufferSize) bufferSize = memory.Length;

            end = end.AppendRented(memory[..bufferSize]);

            memory = memory[bufferSize..];
        } while (memory.Length > 0);

        return new ReadOnlySequence<T>(start, 0, end, end.Memory.Length);
    }
}