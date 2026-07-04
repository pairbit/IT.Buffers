using System.Buffers;

namespace IT.Buffers;

public abstract class GrowableArrayPool<T> : ArrayPool<T>
{
    public abstract T[] RentNext(in ReadOnlySequence<byte> previous);

    public abstract T[] RentNext(int previousLength);
}