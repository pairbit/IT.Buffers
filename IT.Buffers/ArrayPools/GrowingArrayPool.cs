using System;
using System.Buffers;

namespace IT.Buffers;

public abstract class GrowingArrayPool<T> : ArrayPool<T>
{
    public static readonly GrowingArrayPool<T> OneOfEachSize = new DoubleSharedGrowingArrayPool<T>();
    public static readonly GrowingArrayPool<T> TwoOfEachSize = Create(1.4f);
    public static readonly GrowingArrayPool<T> ThreeOfEachSize = Create(1.26f);
    public static readonly GrowingArrayPool<T> FourOfEachSize = Create(1.19f);

    public abstract T[] RentNext(ref int length);

    //public virtual T[] RentNext(in ReadOnlySequence<byte> previous)
    //    => RentNext(BufferSize.GetSizeLastChunk(previous));

    public static GrowingArrayPool<T> Create(float bufferGrowthFactor, int maxBufferSize = BufferSize.Max)
    {
        return new SharedGrowingArrayPool<T>(bufferGrowthFactor, maxBufferSize);
    }
}

internal class SharedGrowingArrayPool<T> : GrowingArrayPool<T>
{
    private readonly float _bufferGrowthFactor;
    private readonly int _maxBufferSize;

    public int MaxBufferSize => _maxBufferSize;

    public float BufferGrowthFactor => _bufferGrowthFactor;

    public SharedGrowingArrayPool(float bufferGrowthFactor, int maxBufferSize)
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

        var newSize = (int)Math.Floor(length * _bufferGrowthFactor);
        var maxSize = _maxBufferSize;
        length = (uint)newSize > maxSize ? maxSize : newSize;

        return array;
    }

    public override T[] Rent(int minimumLength)
        => Shared.Rent(minimumLength);

    public override void Return(T[] array, bool clearArray = false)
        => Shared.Return(array, clearArray);
}

internal class DoubleSharedGrowingArrayPool<T> : GrowingArrayPool<T>
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

internal abstract class GrowingMemoryPool<T> : MemoryPool<T>
{
    public abstract IMemoryOwner<T> RentNext(ref int previousLength);

    //public virtual IMemoryOwner<T> RentNext(in ReadOnlySequence<byte> previous)
    //    => RentNext(BufferSize.GetSizeLastChunk(previous));
}