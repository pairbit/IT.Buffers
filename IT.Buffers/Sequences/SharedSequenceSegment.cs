using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

//TODO: add : SequenceSegment<T>
public class SharedSequenceSegment<T> : ReadOnlySequenceSegment<T>, IResetable
{
    public static BufferPool<SharedSequenceSegment<T>> Pool =>
        NewBufferPool<SharedSequenceSegment<T>>.Shared;

    //TODO: можно определить арендована память по признаку RunningIndex < 0
    private bool _isRentedMemory;

    public bool IsRentedMemory => _isRentedMemory;

    public SharedSequenceSegment()
    {

    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new ReadOnlyMemory<T> Memory
    {
        get => base.Memory;
        set => base.Memory = value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public new SharedSequenceSegment<T>? Next
    {
        get => (SharedSequenceSegment<T>?)base.Next;
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
        _isRentedMemory = isRented;
    }

    public SharedSequenceSegment<T> AppendRented(ReadOnlyMemory<T> memory, bool isRented = false)
    {
        var next = Pool.Rent();

        next.SetMemory(memory, isRented);
        next.RunningIndex = RunningIndex + Memory.Length;

        Next = next;

        return next;
    }

    public void Reset()
    {
        if (_isRentedMemory)
        {
            var returned = BufferPool.TryReturn(base.Memory);
            Debug.Assert(returned);
        }
        _isRentedMemory = false;
        base.Memory = default;
        base.RunningIndex = 0;
        base.Next = null;
    }
}