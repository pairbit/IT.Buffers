using IT.Buffers.Interfaces;
using System;
using System.Buffers;

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
        var next = SequenceSegment<T>.Pool.Rent();

        next.SetMemory(memory, isRented);
        next.RunningIndex = segment.RunningIndex + segment.Memory.Length;

        segment.Next = next;

        return next;
    }
}