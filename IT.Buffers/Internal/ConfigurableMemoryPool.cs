using System;
using System.Buffers;

namespace IT.Buffers;

internal sealed class ConfigurableMemoryPool<T> : MemoryPool<T>
{
    private readonly ArrayPool<T> _pool;
    private readonly int _maxBufferSize;
    private readonly int _defaultBufferSize;
    private readonly bool _clearArray;

    public override int MaxBufferSize => _maxBufferSize;

    public ConfigurableMemoryPool(ArrayPool<T> pool, int defaultBufferSize, int maxBufferSize, bool clearArray)
    {
        _pool = pool;
        _defaultBufferSize = defaultBufferSize;
        _maxBufferSize = maxBufferSize;
        _clearArray = clearArray;
    }

    public override IMemoryOwner<T> Rent(int minBufferSize = -1)
    {
        if (minBufferSize == -1)
            return new MemoryOwner(this, _defaultBufferSize);

        if (((uint)minBufferSize) > _maxBufferSize)
            throw new ArgumentOutOfRangeException(nameof(minBufferSize));

        return new MemoryOwner(this, minBufferSize);
    }

    protected override void Dispose(bool disposing) { }

    private sealed class MemoryOwner : IMemoryOwner<T>
    {
        private readonly ConfigurableMemoryPool<T> _pool;
        private T[]? _array;

        public MemoryOwner(ConfigurableMemoryPool<T> pool, int size)
        {
            _pool = pool;
            _array = pool._pool.Rent(size);
        }

        public Memory<T> Memory => _array ?? throw new ObjectDisposedException(nameof(MemoryOwner));

        public void Dispose()
        {
            var array = _array;
            if (array != null)
            {
                _array = null;
                _pool._pool.Return(array, _pool._clearArray);
            }
        }
    }
}