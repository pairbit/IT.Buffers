using System;
using System.Buffers;

namespace IT.Buffers.Extensions;

public static class xReadOnlyMemory
{
    public static ReadOnlySequence<T> SplitBySegments<T>(this Memory<T> memory, int maxSegments)
        => SplitBySegments((ReadOnlyMemory<T>)memory, maxSegments);

    public static ReadOnlySequence<T> SplitBySegments<T>(this ReadOnlyMemory<T> memory, int maxSegments)
    {
        if (maxSegments <= 0) throw new ArgumentOutOfRangeException(nameof(maxSegments));

        var length = memory.Length;
        if (length == 0) return ReadOnlySequence<T>.Empty;

        var segments = length < maxSegments ? length : maxSegments;
        if (segments == 1) return new(memory);

        var segmentLength = length / segments;

        var start = new SequenceSegment<T>
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


    public static ReadOnlySequence<T> Split<T>(this Memory<T> memory,
        int bufferSize, BufferGrowthPolicy growthPolicy = BufferGrowthPolicy.Double)
        => Split((ReadOnlyMemory<T>)memory, bufferSize, growthPolicy);

    public static ReadOnlySequence<T> SplitAndRent<T>(this Memory<T> memory,
        int bufferSize, BufferGrowthPolicy growthPolicy = BufferGrowthPolicy.Double,
        bool isRented = false)
        => SplitAndRent((ReadOnlyMemory<T>)memory, bufferSize, growthPolicy, isRented);

    public static ReadOnlySequence<T> Split<T>(this ReadOnlyMemory<T> memory,
        int bufferSize, BufferGrowthPolicy growthPolicy = BufferGrowthPolicy.Double)
    {
        if (memory.IsEmpty) return ReadOnlySequence<T>.Empty;

        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        if (bufferSize >= memory.Length) return new ReadOnlySequence<T>(memory);

        var start = new SequenceSegment<T>
        {
            Memory = memory[..bufferSize]
        };

        memory = memory[bufferSize..];
        var end = start;

        do
        {
            if (growthPolicy == BufferGrowthPolicy.Double)
                bufferSize = BufferSize.GetDoubleCapacity(bufferSize);

            if (memory.Length < bufferSize) bufferSize = memory.Length;

            end = end.Append(memory[..bufferSize]);

            memory = memory[bufferSize..];
        } while (memory.Length > 0);

        return new ReadOnlySequence<T>(start, 0, end, end.Memory.Length);
    }

    public static ReadOnlySequence<T> SplitAndRent<T>(this ReadOnlyMemory<T> memory,
        int bufferSize, BufferGrowthPolicy growthPolicy = BufferGrowthPolicy.Double,
        bool isRented = false)
    {
        if (memory.IsEmpty) return ReadOnlySequence<T>.Empty;

        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        if (bufferSize >= memory.Length)
        {
            //If the memory is rented, then we expect to wrap the segment,
            //otherwise the memory will not be returned to the pool
            if (isRented)
            {
                var single = BufferPool<SequenceSegment<T>>.Shared.Rent();
                single.SetMemory(memory, isRented: true);
                return new ReadOnlySequence<T>(single, 0, single, memory.Length);
            }

            return new ReadOnlySequence<T>(memory);
        }

        var start = BufferPool<SequenceSegment<T>>.Shared.Rent();
        start.SetMemory(memory[..bufferSize], isRented);

        memory = memory[bufferSize..];
        var end = start;

        do
        {
            if (growthPolicy == BufferGrowthPolicy.Double)
                bufferSize = BufferSize.GetDoubleCapacity(bufferSize);

            if (memory.Length < bufferSize) bufferSize = memory.Length;

            end = end.AppendRented(memory[..bufferSize]);

            memory = memory[bufferSize..];
        } while (memory.Length > 0);

        return new ReadOnlySequence<T>(start, 0, end, end.Memory.Length);
    }
}