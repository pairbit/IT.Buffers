using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers.Internal;

//TODO: add : SequenceSegment<T>
internal class SimpleSequenceSegment<T> : ReadOnlySequenceSegment<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new ReadOnlyMemory<T> Memory
    {
        get => base.Memory;
        set => base.Memory = value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new SimpleSequenceSegment<T>? Next
    {
        get => (SimpleSequenceSegment<T>?)base.Next;
        set => base.Next = value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new long RunningIndex
    {
        get => base.RunningIndex;
        set => base.RunningIndex = value;
    }

    public SimpleSequenceSegment<T> Append(ReadOnlyMemory<T> memory)
    {
        var next = new SimpleSequenceSegment<T>
        {
            Memory = memory,
            RunningIndex = RunningIndex + Memory.Length
        };

        Next = next;

        return next;
    }
}