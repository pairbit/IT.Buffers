using System;

namespace IT.Buffers.Extensions;

public static class xSequenceSegment
{
    public static SequenceSegment<T> Append<T>(this SequenceSegment<T> segment, ReadOnlyMemory<T> memory)
    {
        var next = new SequenceSegment<T>
        {
            Memory = memory,
            RunningIndex = segment.RunningIndex + segment.Memory.Length
        };

        segment.Next = next;

        return next;
    }

    public static SequenceSegment<T> AppendRented<T>(this SequenceSegment<T> segment, ReadOnlyMemory<T> memory,
        bool isRented = false)
    {
        var next = BufferPool<SequenceSegment<T>>.Shared.Rent();

        next.SetMemory(memory, isRented);
        next.RunningIndex = segment.RunningIndex + segment.Memory.Length;

        segment.Next = next;

        return next;
    }
}