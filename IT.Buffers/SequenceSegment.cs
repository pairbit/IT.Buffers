using IT.Buffers.Interfaces;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public class SequenceSegment<T> : ReadOnlySequenceSegment<T>, IDisposable, IBufferRentable
{
    public static BufferPool<SequenceSegment<T>> Pool
        => BufferPool<SequenceSegment<T>>.Shared;

    private RentalStatus _rentalStatus;

    bool IBufferRentable.IsRented => IsRentedSegment;

    public bool IsRentedMemory => (_rentalStatus & RentalStatus.Memory) == RentalStatus.Memory;

    public bool IsRentedSegment => (_rentalStatus & RentalStatus.Segment) == RentalStatus.Segment;

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

        if (isRented)
            _rentalStatus |= RentalStatus.Memory;
    }

    public void Reset()
    {
        if (IsRentedMemory)
        {
            var returned = BufferPool.TryReturn(base.Memory);
            Debug.Assert(returned);
        }
        _rentalStatus = default;
        base.Memory = default;
        base.RunningIndex = 0;
        base.Next = null;
    }

    void IBufferRentable.MakeRented()
    {
        _rentalStatus |= RentalStatus.Segment;
    }

    void IDisposable.Dispose() => Reset();

    [Flags]
    enum RentalStatus : byte
    {
        None = 0,
        Memory = 1,
        Segment = 2
    }
}