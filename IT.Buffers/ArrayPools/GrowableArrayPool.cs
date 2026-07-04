using System;
using System.Buffers;

namespace IT.Buffers;

internal abstract class GrowableArrayPool<T> : ArrayPool<T>
{
    public static readonly GrowableArrayPool<T> OneOfEachSize = new DoubleSharedGrowableArrayPool<T>();
    public static readonly GrowableArrayPool<T> TwoOfEachSize = new SharedGrowableArrayPool<T>(1.4f);
    public static readonly GrowableArrayPool<T> FourOfEachSize = new SharedGrowableArrayPool<T>(1.19f);

    public virtual int MaxBufferSize => BufferSize.Max;

    public virtual float BufferGrowthFactor => 2;

    //public virtual T[] RentNext(in ReadOnlySequence<byte> previous)
    //    => RentNext(BufferSize.GetSizeLastChunk(previous));

    public virtual T[] RentNext(ref int length)
    {
        var array = Rent(length);

        var newSize = (int)Math.Floor(length * BufferGrowthFactor);
        var maxSize = MaxBufferSize;
        length = (uint)newSize > maxSize ? maxSize : newSize;

        return array;
    }
}

internal class SharedGrowableArrayPool<T> : GrowableArrayPool<T>
{
    private readonly float _bufferGrowthFactor;

    public override float BufferGrowthFactor => _bufferGrowthFactor;

    public SharedGrowableArrayPool(float bufferGrowthFactor)
    {
        if (bufferGrowthFactor <= 0) throw new ArgumentOutOfRangeException(nameof(bufferGrowthFactor));

        _bufferGrowthFactor = bufferGrowthFactor;
    }

    public override T[] Rent(int minimumLength)
        => Shared.Rent(minimumLength);

    public override void Return(T[] array, bool clearArray = false)
        => Shared.Return(array, clearArray);
}

internal class DoubleSharedGrowableArrayPool<T> : GrowableArrayPool<T>
{
    public override T[] RentNext(ref int length)
    {
        var array = Shared.Rent(length);

        length = BufferSize.GetDoubleCapacity(length, MaxBufferSize);

        return array;
    }

    public override T[] Rent(int minimumLength)
        => Shared.Rent(minimumLength);

    public override void Return(T[] array, bool clearArray = false)
        => Shared.Return(array, clearArray);
}

internal abstract class GrowableMemoryPool<T> : MemoryPool<T>
{
    public virtual float BufferGrowthFactor => 2;

    //public virtual IMemoryOwner<T> RentNext(in ReadOnlySequence<byte> previous)
    //    => RentNext(BufferSize.GetSizeLastChunk(previous));

    public virtual IMemoryOwner<T> RentNext(ref int previousLength)
        => Rent(BufferSize.GetDoubleCapacity(previousLength, MaxBufferSize));
}