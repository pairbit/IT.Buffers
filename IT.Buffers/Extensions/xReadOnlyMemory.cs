using System;
using System.Buffers;

namespace IT.Buffers.Extensions;

public static class xReadOnlyMemory
{
    public static ReadOnlySequence<T> Split<T>(this Memory<T> memory,
        int bufferSize = BufferSize.KB_64, BufferGrowthPolicy growthPolicy = BufferGrowthPolicy.Double)
        => Split((ReadOnlyMemory<T>)memory, bufferSize, growthPolicy);

    public static ReadOnlySequence<T> Split<T>(this ReadOnlyMemory<T> memory, 
        int bufferSize = BufferSize.KB_64, BufferGrowthPolicy growthPolicy = BufferGrowthPolicy.Double)
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
        int bufferSize = BufferSize.KB_64, BufferGrowthPolicy growthPolicy = BufferGrowthPolicy.Double)
    {
        if (memory.IsEmpty) return ReadOnlySequence<T>.Empty;

        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        if (bufferSize >= memory.Length) return new ReadOnlySequence<T>(memory);

        var start = SequenceSegment<T>.Pool.Rent();
        start.SetMemory(memory[..bufferSize]);

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