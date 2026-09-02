using System;

namespace IT.Buffers.Extensions;

public static class xSequenceSegment
{
    public static RentableSequenceSegment<T> Append<T>(this RentableSequenceSegment<T> segment, ReadOnlyMemory<T> memory)
    {
        var next = new RentableSequenceSegment<T>
        {
            Memory = memory,
            RunningIndex = segment.RunningIndex + segment.Memory.Length
        };

        segment.Next = next;

        return next;
    }

    public static RentableSequenceSegment<T> AppendRented<T>(this RentableSequenceSegment<T> segment, ReadOnlyMemory<T> memory,
        bool isRented = false)
    {
        var next = BufferPool<RentableSequenceSegment<T>>.Shared.Rent();

        next.SetMemory(memory, isRented);
        next.RunningIndex = segment.RunningIndex + segment.Memory.Length;

        segment.Next = next;

        return next;
    }
}