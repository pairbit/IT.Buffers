using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public abstract class SequenceSegment<T> : ReadOnlySequenceSegment<T>
{
    public SequenceSegment<T>? Previous { get; protected set; }

    public new SequenceSegment<T>? Next
    {
        get => (SequenceSegment<T>?)base.Next;
        protected set => base.Next = value;
    }

    public new Memory<T> Memory
    {
        get => MemoryMarshal.AsMemory(base.Memory);
        protected set => MemoryMarshal.AsMemory(base.Memory);
    }
}