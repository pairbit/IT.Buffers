using IT.Buffers.Interfaces;
using System;

namespace IT.Buffers;

public class BufferGrowthStrategy : IBufferGrowthStrategy
{
    public static readonly IBufferGrowthStrategy OneOfEachSize = Create(2f);
    public static readonly IBufferGrowthStrategy TwoOfEachSize = Create(1.4f);
    public static readonly IBufferGrowthStrategy ThreeOfEachSize = Create(1.26f);
    public static readonly IBufferGrowthStrategy FourOfEachSize = Create(1.19f);

    private readonly float _bufferGrowthFactor;
    private readonly int _maxBufferSize;

    public int MaxBufferSize => _maxBufferSize;

    public float BufferGrowthFactor => _bufferGrowthFactor;

    private BufferGrowthStrategy(float bufferGrowthFactor)
    {
        if (bufferGrowthFactor <= 0) throw new ArgumentOutOfRangeException(nameof(bufferGrowthFactor));

        _bufferGrowthFactor = bufferGrowthFactor;
        _maxBufferSize = BufferSize.Max;
    }

    private BufferGrowthStrategy(float bufferGrowthFactor, int maxBufferSize)
    {
        if (bufferGrowthFactor <= 0) throw new ArgumentOutOfRangeException(nameof(bufferGrowthFactor));
        if (maxBufferSize < 0 || maxBufferSize > BufferSize.Max)
            throw new ArgumentOutOfRangeException(nameof(maxBufferSize));

        _bufferGrowthFactor = bufferGrowthFactor;
        _maxBufferSize = maxBufferSize;
    }

    public int Grow(int size)
    {
        var newSize = (int)Math.Floor(size * _bufferGrowthFactor);
        var maxSize = _maxBufferSize;
        return (uint)newSize > maxSize ? maxSize : newSize;
    }

    public static IBufferGrowthStrategy Create(float bufferGrowthFactor)
    {
        return new BufferGrowthStrategy(bufferGrowthFactor);
    }

    public static IBufferGrowthStrategy Create(float bufferGrowthFactor, int maxBufferSize)
    {
        return new BufferGrowthStrategy(bufferGrowthFactor, maxBufferSize);
    }
}