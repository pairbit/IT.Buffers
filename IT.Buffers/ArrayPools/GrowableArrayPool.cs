using System;
using System.Buffers;

namespace IT.Buffers;

internal abstract class GrowableArrayPool<T> : ArrayPool<T>
{
    public static new readonly GrowableArrayPool<T> Shared = new SharedGrowableArrayPool<T>();

    public virtual int MaxBufferSize => BufferSize.Max;

    public virtual float BufferGrowthFactor => 2;

    //public virtual T[] RentNext(in ReadOnlySequence<byte> previous)
    //    => RentNext(BufferSize.GetSizeLastChunk(previous));

    public virtual T[] RentNext(ref int length)
    {
        var newSize = (int)Math.Floor(length * BufferGrowthFactor);
        var maxSize = MaxBufferSize;
        length = (uint)newSize > maxSize ? maxSize : newSize;

        return Rent(length);
    }
}

internal class SharedGrowableArrayPool<T> : GrowableArrayPool<T>
{
    public override T[] RentNext(ref int nextLength)
    {
        var array = ArrayPool<T>.Shared.Rent(nextLength);

        nextLength = BufferSize.GetDoubleCapacity(nextLength, MaxBufferSize);

        return array;
    }

    public override T[] Rent(int minimumLength)
        => ArrayPool<T>.Shared.Rent(minimumLength);

    public override void Return(T[] array, bool clearArray = false)
        => ArrayPool<T>.Shared.Return(array, clearArray);
}

internal abstract class GrowableMemoryPool<T> : MemoryPool<T>
{
    public virtual float BufferGrowthFactor => 2;

    //public virtual IMemoryOwner<T> RentNext(in ReadOnlySequence<byte> previous)
    //    => RentNext(BufferSize.GetSizeLastChunk(previous));

    public virtual IMemoryOwner<T> RentNext(ref int previousLength)
        => Rent(BufferSize.GetDoubleCapacity(previousLength, MaxBufferSize));
}