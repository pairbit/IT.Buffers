using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public class SequenceSegment<T> : ReadOnlySequenceSegment<T>, IDisposable
{
    public static BufferPool<SequenceSegment<T>> Pool
        => BufferPool<SequenceSegment<T>>.Shared;

    private bool _returnToPool;

    public SequenceSegment<T>? GetNext() => (SequenceSegment<T>?)Next;

    public void SetMemory(ReadOnlyMemory<T> memory, bool returnToPool = false)
    {
        Memory = memory;
        _returnToPool = returnToPool;
    }

    public void SetRunningIndexAndNext(long runningIndex, SequenceSegment<T>? next)
    {
        RunningIndex = runningIndex;
        Next = next;
    }

    public void Reset()
    {
        if (_returnToPool)
        {
            Debug.Assert(ArrayPoolShared.TryReturnAndClear(Memory));
        }
        Memory = default;
        RunningIndex = 0;
        Next = null;
    }

    void IDisposable.Dispose() => Reset();
}