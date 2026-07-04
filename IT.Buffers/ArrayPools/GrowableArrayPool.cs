using System;
using System.Buffers;

namespace IT.Buffers;

public abstract class GrowableArrayPool<T> : ArrayPool<T>
{
    public static readonly GrowableArrayPool<T> OneOfEachSize = new DoubleSharedGrowableArrayPool<T>();
    public static readonly GrowableArrayPool<T> TwoOfEachSize = Create(1.4f);
    public static readonly GrowableArrayPool<T> ThreeOfEachSize = Create(1.26f);
    public static readonly GrowableArrayPool<T> FourOfEachSize = Create(1.19f);

    public abstract T[] RentNext(ref int length);

    //public virtual T[] RentNext(in ReadOnlySequence<byte> previous)
    //    => RentNext(BufferSize.GetSizeLastChunk(previous));

    public static GrowableArrayPool<T> Create(float bufferGrowthFactor, int maxBufferSize = BufferSize.Max)
    {
        return new SharedGrowableArrayPool<T>(bufferGrowthFactor, maxBufferSize);
    }
}

internal class SharedGrowableArrayPool<T> : GrowableArrayPool<T>
{
    private readonly float _bufferGrowthFactor;
    private readonly int _maxBufferSize;

    public int MaxBufferSize => _maxBufferSize;

    public float BufferGrowthFactor => _bufferGrowthFactor;

    public SharedGrowableArrayPool(float bufferGrowthFactor, int maxBufferSize)
    {
        if (bufferGrowthFactor <= 0) throw new ArgumentOutOfRangeException(nameof(bufferGrowthFactor));
        if (maxBufferSize < 0 || maxBufferSize > BufferSize.Max)
            throw new ArgumentOutOfRangeException(nameof(maxBufferSize));

        _bufferGrowthFactor = bufferGrowthFactor;
        _maxBufferSize = maxBufferSize;
    }

    public override T[] RentNext(ref int length)
    {
        var array = Rent(length);

        var newSize = (int)Math.Floor(length * BufferGrowthFactor);
        var maxSize = _maxBufferSize;
        length = (uint)newSize > maxSize ? maxSize : newSize;

        return array;
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

        length = BufferSize.GetDoubleCapacity(length);

        return array;
    }

    public override T[] Rent(int minimumLength)
        => Shared.Rent(minimumLength);

    public override void Return(T[] array, bool clearArray = false)
        => Shared.Return(array, clearArray);
}

internal abstract class GrowableMemoryPool<T> : MemoryPool<T>
{
    public abstract IMemoryOwner<T> RentNext(ref int previousLength);

    //public virtual IMemoryOwner<T> RentNext(in ReadOnlySequence<byte> previous)
    //    => RentNext(BufferSize.GetSizeLastChunk(previous));
}