using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public abstract class SequenceSegment<T> : ReadOnlySequenceSegment<T>
{
    //TODO: нужен ли предыдущий кусок?
    //private SequenceSegment<T>? _previous;

    //public SequenceSegment<T>? Previous => _previous;

    public new SequenceSegment<T>? Next
    {
        get => (SequenceSegment<T>?)base.Next;
        protected set => base.Next = value;
    }

    public new Memory<T> Memory
    {
        get => MemoryMarshal.AsMemory(base.Memory);
        protected set => base.Memory = value;
    }

    protected void Reset()
    {
        base.Next = null;
        base.Memory = default;
        RunningIndex = 0;
    }
}