using System.Buffers;

namespace IT.Buffers;

internal abstract class GrowableArrayPool<T> : ArrayPool<T>
{
    public virtual int MaxBufferSize => BufferSize.Max;

    public virtual T[] RentNext(in ReadOnlySequence<byte> previous)
        => RentNext(BufferSize.GetSizeLastChunk(previous));

    public virtual T[] RentNext(int previousLength)
        => Rent(BufferSize.GetDoubleCapacity(previousLength, MaxBufferSize));
}

internal abstract class GrowableMemoryPool<T> : MemoryPool<T>
{
    public virtual IMemoryOwner<T> RentNext(in ReadOnlySequence<byte> previous)
        => RentNext(BufferSize.GetSizeLastChunk(previous));

    public virtual IMemoryOwner<T> RentNext(int previousLength)
        => Rent(BufferSize.GetDoubleCapacity(previousLength, MaxBufferSize));
}