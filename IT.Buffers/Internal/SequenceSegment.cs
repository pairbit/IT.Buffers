using IT.Buffers.Extensions;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace IT.Buffers.Internal;

internal class SequenceSegment<T> : ReadOnlySequenceSegment<T>
{
    private bool _returnToPool;

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
            if (MemoryMarshal.TryGetArray(Memory, out var segment))
            {
                var array = segment.Array;
                if (array != null && array.Length > 0)
                {
                    ArrayPool<T>.Shared.ReturnAndClear(array);
                }
            }
        }
        Memory = default;
        RunningIndex = 0;
        Next = null;
    }
}