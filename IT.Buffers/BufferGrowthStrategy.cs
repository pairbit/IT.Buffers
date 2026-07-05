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
    private readonly int _firstBufferSize;
    private readonly int _nextBufferSize;
    private readonly int _maxBufferSize;

    public int FirstBufferSize => _firstBufferSize;

    public int NextBufferSize => _nextBufferSize;

    protected BufferGrowthStrategy(float bufferGrowthFactor, int firstBufferSize, int nextBufferSize, int maxBufferSize)
    {
        if (bufferGrowthFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferGrowthFactor));

        if (maxBufferSize < 0 || maxBufferSize > BufferSize.Max)
            throw new ArgumentOutOfRangeException(nameof(maxBufferSize));

        if (firstBufferSize < 0 || firstBufferSize > maxBufferSize)
            throw new ArgumentOutOfRangeException(nameof(firstBufferSize));

        if (nextBufferSize < 0 || nextBufferSize > maxBufferSize)
            throw new ArgumentOutOfRangeException(nameof(nextBufferSize));

        _bufferGrowthFactor = bufferGrowthFactor;
        _firstBufferSize = firstBufferSize;
        _nextBufferSize = nextBufferSize;
        _maxBufferSize = maxBufferSize;
    }

    public virtual int Grow(int size)
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

        return new BufferGrowthStrategy(bufferGrowthFactor, BufferSize.KB_16, BufferSize.KB_8, BufferSize.Max);
    }

    public static IBufferGrowthStrategy Create(
        float bufferGrowthFactor,
        int firstBufferSize = BufferSize.KB_16,
        int nextBufferSize = BufferSize.KB_8,
        int maxBufferSize = BufferSize.Max)
    {
        //if (bufferGrowthFactor == 1)
        //    return Single.Instance;

        //if (bufferGrowthFactor == 2)
        //    return Double.Instance;

        return new BufferGrowthStrategy(bufferGrowthFactor, firstBufferSize, nextBufferSize, maxBufferSize);
    }

    class Single : IBufferGrowthStrategy
    {
        public static readonly Single Instance = new();

        public int FirstBufferSize => BufferSize.KB_64;

        public int NextBufferSize => BufferSize.KB_64;

        private Single() { }

        public int Grow(int size) => size;
    }

    class Double : IBufferGrowthStrategy
    {
        public static readonly Double Instance = new();

        private Double() { }

        public int FirstBufferSize => BufferSize.KB_16;

        public int NextBufferSize => BufferSize.KB_8;

        public int Grow(int size)
        {
            return BufferSize.GetDoubleCapacity(size);
        }
    }
}