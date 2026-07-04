using System.Buffers;

namespace IT.Buffers;

public abstract class GrowableArrayPool<T> : ArrayPool<T>
{
    public virtual T[] RentNext(in ReadOnlySequence<byte> previous)
        => RentNext(BufferSize.GetSizeLastChunk(previous));

    public virtual T[] RentNext(int previousLength)
        => Rent(BufferSize.GetDoubleCapacity(previousLength));
}