using IT.Buffers.Tests.Internal.Iteration;

namespace IT.Buffers.Tests.Iteration;

internal struct MemoriesIterator<T> : IMemoryIterator<T>
{
    private readonly Memory<T>[] _array;
    public int _index;

    public MemoriesIterator(params Memory<T>[] array)
    {
        _array = array;
        _index = 0;
    }

    public bool TryGetNextMemory(out Memory<T> memory)
    {
        if (_index < _array.Length)
        {
            memory = _array[_index++];
            return true;
        }
        memory = default;
        return false;
    }

    public bool TryGetNextSpan(out Span<T> span)
    {
        if (_index < _array.Length)
        {
            span = _array[_index++].Span;
            return true;
        }
        span = default;
        return false;
    }

    bool IReadOnlySpanIterator<T>.TryGetNextSpan(out ReadOnlySpan<T> span)
    {
        if (_index < _array.Length)
        {
            span = _array[_index++].Span;
            return true;
        }
        span = default;
        return false;
    }

    bool IReadOnlyMemoryIterator<T>.TryGetNextMemory(out ReadOnlyMemory<T> memory)
    {
        if (_index < _array.Length)
        {
            memory = _array[_index++];
            return true;
        }
        memory = default;
        return false;
    }
}