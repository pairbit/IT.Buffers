using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public class SequenceSegment<T> : ReadOnlySequenceSegment<T>, IDisposable
{
    public static BufferPool<SequenceSegment<T>> Pool
        => BufferPool<SequenceSegment<T>>.Shared;

    private bool _isRented;

    public bool IsRented => _isRented;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new ReadOnlyMemory<T> Memory
    {
        get => base.Memory;
        set => base.Memory = value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new SequenceSegment<T>? Next
    {
        get => (SequenceSegment<T>?)base.Next;
        set => base.Next = value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new long RunningIndex
    {
        get => base.RunningIndex;
        set => base.RunningIndex = value;
    }

    public void SetMemory(ReadOnlyMemory<T> memory, bool isRented = false)
    {
        base.Memory = memory;
        _isRented = isRented;
    }

    public void Reset()
    {
        if (_isRented)
        {
            var returned = ArrayPoolShared.TryReturn(base.Memory);
            Debug.Assert(returned);
            _isRented = false;
        }
        base.Memory = default;
        base.RunningIndex = 0;
        base.Next = null;
    }

    void IDisposable.Dispose() => Reset();
}