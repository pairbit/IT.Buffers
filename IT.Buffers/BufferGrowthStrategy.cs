using IT.Buffers.Interfaces;
using System;
using System.Diagnostics;

namespace IT.Buffers;

public class BufferGrowthStrategy : IBufferGrowthStrategy
{
    public static readonly IBufferGrowthStrategy Off = Create(1);
    public static readonly IBufferGrowthStrategy OneOfEachSize = Create(2f);
    public static readonly IBufferGrowthStrategy TwoOfEachSize = Create(1.4f);
    public static readonly IBufferGrowthStrategy ThreeOfEachSize = Create(1.26f);
    public static readonly IBufferGrowthStrategy FourOfEachSize = Create(1.19f);

    private readonly float _bufferGrowthFactor;
    private readonly int _bufferSize;
    private readonly int _maxBufferSize;

    public int GetBufferSize<T>() => BufferSize<T>.Get(_bufferSize);

    protected BufferGrowthStrategy(float bufferGrowthFactor, int bufferSize, int maxBufferSize)
    {
        if (bufferGrowthFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferGrowthFactor));

        if (maxBufferSize <= 0 || maxBufferSize > BufferSize.Max)
            throw new ArgumentOutOfRangeException(nameof(maxBufferSize));

        if (bufferSize <= 0 || bufferSize > maxBufferSize)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        _bufferGrowthFactor = bufferGrowthFactor;
        _bufferSize = bufferSize;
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

        return new BufferGrowthStrategy(bufferGrowthFactor, BufferSize.KB_4, BufferSize.Max);
    }

    public static IBufferGrowthStrategy Create(float bufferGrowthFactor, int bufferSize,
        int maxBufferSize = BufferSize.Max)
    {
        if (bufferGrowthFactor == 1)
        {
            if (bufferSize <= 0 || bufferSize > maxBufferSize)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            return new SingleWithSize(bufferSize);
        }

        if (bufferGrowthFactor == 2)
            return new DoubleWithSize(bufferSize, maxBufferSize);

        return new BufferGrowthStrategy(bufferGrowthFactor, bufferSize, maxBufferSize);
    }

    class Single : IBufferGrowthStrategy
    {
        public static readonly Single Instance = new();

        private Single() { }

        public int GetBufferSize<T>() => BufferSize<T>.KB_4;

        public int Grow(int size) => size;
    }

    class SingleWithSize : IBufferGrowthStrategy
    {
        private readonly int _bufferSize;

        public SingleWithSize(int bufferSize)
        {
            Debug.Assert(bufferSize > 0 && bufferSize <= BufferSize.Max);

            _bufferSize = bufferSize;
        }

        public int GetBufferSize<T>() => BufferSize<T>.Get(_bufferSize);

        public int Grow(int size) => size;
    }

    class Double : IBufferGrowthStrategy
    {
        public static readonly Double Instance = new();

        private Double() { }

        public int GetBufferSize<T>() => BufferSize<T>.KB_4;

        public int Grow(int size) => BufferSize.GetDoubleCapacity(size);
    }

    class DoubleWithSize : IBufferGrowthStrategy
    {
        private readonly int _bufferSize;
        private readonly int _maxBufferSize;

        public DoubleWithSize(int bufferSize, int maxBufferSize)
        {
            if (maxBufferSize <= 0 || maxBufferSize > BufferSize.Max)
                throw new ArgumentOutOfRangeException(nameof(maxBufferSize));

            if (bufferSize <= 0 || bufferSize > maxBufferSize)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _bufferSize = bufferSize;
            _maxBufferSize = maxBufferSize;
        }

        public int GetBufferSize<T>() => BufferSize<T>.Get(_bufferSize);

        public int Grow(int size) => BufferSize.GetDoubleCapacity(size, _maxBufferSize);
    }
}