using IT.Buffers.Interfaces;
using System;

namespace IT.Buffers;

public class BufferGrowthStrategy : IBufferGrowthStrategy
{
    public static readonly IBufferGrowthStrategy Off = Create(1);
    public static readonly IBufferGrowthStrategy OneOfEachSize = Create(2f);
    public static readonly IBufferGrowthStrategy TwoOfEachSize = Create(1.4f);
    public static readonly IBufferGrowthStrategy ThreeOfEachSize = Create(1.26f);
    public static readonly IBufferGrowthStrategy FourOfEachSize = Create(1.19f);

    private readonly float _bufferGrowthFactor;
    private readonly int _maxBufferSize;

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
        if (bufferGrowthFactor == 1)
            return Single.Instance;

        if (bufferGrowthFactor == 2)
            return Double.Instance;

        return new BufferGrowthStrategy(bufferGrowthFactor);
    }

    public static IBufferGrowthStrategy Create(float bufferGrowthFactor, int maxBufferSize)
    {
        if (bufferGrowthFactor == 1)
            return Single.Instance;

        if (bufferGrowthFactor == 2)
            return new DoubleWithMax(maxBufferSize);

        return new BufferGrowthStrategy(bufferGrowthFactor, maxBufferSize);
    }

    internal class Single : IBufferGrowthStrategy
    {
        public static readonly Single Instance = new();

        private Single() { }

        public int Grow(int size) => size;
    }

    internal class Double : IBufferGrowthStrategy
    {
        public static readonly Double Instance = new();

        private Double() { }

        public int Grow(int size)
        {
            return BufferSize.GetDoubleCapacity(size);
        }
    }

    internal class DoubleWithMax : IBufferGrowthStrategy
    {
        private readonly int _maxBufferSize;

        public DoubleWithMax(int maxBufferSize)
        {
            if (maxBufferSize < 0 || maxBufferSize > BufferSize.Max)
                throw new ArgumentOutOfRangeException(nameof(maxBufferSize));

            _maxBufferSize = maxBufferSize;
        }

        public int Grow(int size)
        {
            return BufferSize.GetDoubleCapacity(size, _maxBufferSize);
        }
    }
}