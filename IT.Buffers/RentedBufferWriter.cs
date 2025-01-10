using IT.Buffers.Extensions;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public sealed class RentedBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private T[]? _rentedBuffer;
    private int _index;

    public RentedBufferWriter()
    {
        _rentedBuffer = ArrayPool<T>.Shared.Rent(BufferSize.Min);
        _index = 0;
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public RentedBufferWriter(int initialCapacity)
    {
        if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        if (initialCapacity == 0) initialCapacity = BufferSize.Min;

        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            Debug.Assert(_index <= _rentedBuffer.Length);

            return _rentedBuffer.AsMemory(0, _index);
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            Debug.Assert(_index <= _rentedBuffer.Length);

            return _rentedBuffer.AsSpan(0, _index);
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public int WrittenCount
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            return _index;
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public int Capacity
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            return _rentedBuffer.Length;
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public int FreeCapacity
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            return _rentedBuffer.Length - _index;
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void Clear()
    {
        var rentedBuffer = _rentedBuffer ?? throw Disposed();

        Debug.Assert(rentedBuffer != null);
        Debug.Assert(_index <= rentedBuffer.Length);

        rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void ResetWrittenCount()
    {
        if (_rentedBuffer == null) throw Disposed();

        _index = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void Return()
    {
        var rentedBuffer = _rentedBuffer ?? throw Disposed();
        _rentedBuffer = null;
        ArrayPool<T>.Shared.ReturnAndClear(rentedBuffer);
    }

    public void Dispose()
    {
        var rentedBuffer = _rentedBuffer;
        if (rentedBuffer == null) return;

        _rentedBuffer = null;
        ArrayPool<T>.Shared.ReturnAndClear(rentedBuffer);
    }

    /// <exception cref="InvalidOperationException"></exception>
    public void Initialize()
    {
        if (_rentedBuffer != null) throw new InvalidOperationException("buffer not returned");

        _rentedBuffer = ArrayPool<T>.Shared.Rent(BufferSize.Min);
        _index = 0;
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Initialize(int initialCapacity)
    {
        if (_rentedBuffer != null) throw new InvalidOperationException("buffer not returned");
        if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        if (initialCapacity == 0) initialCapacity = BufferSize.Min;

        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void Advance(int count)
    {
        if (_rentedBuffer == null) throw Disposed();

        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

        if (_index > _rentedBuffer.Length - count)
            throw new ArgumentOutOfRangeException(nameof(count));

        _index += count;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsSpan(_index);
    }

    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    private void CheckAndResizeBuffer(int sizeHint)
    {
        if (_rentedBuffer == null) throw Disposed();

        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        if (sizeHint == 0) sizeHint = BufferSize.Min;

        int currentLength = _rentedBuffer.Length;
        int availableSpace = currentLength - _index;

        // If we've reached ~1GB written, grow to the maximum buffer
        // length to avoid incessant minimal growths causing perf issues.
        if (_index >= BufferSize.MaxHalf)
        {
            sizeHint = Math.Max(sizeHint, BufferSize.Max - currentLength);
        }

        if (sizeHint > availableSpace)
        {
            int growBy = Math.Max(sizeHint, currentLength);

            int newSize = currentLength + growBy;

            if ((uint)newSize > BufferSize.Max)
            {
                newSize = currentLength + sizeHint;
                if ((uint)newSize > BufferSize.Max)
                {
                    throw new OutOfMemoryException($"Size {(uint)newSize} > {BufferSize.Max}");
                }
            }

            T[] oldBuffer = _rentedBuffer;

            _rentedBuffer = ArrayPool<T>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _index);
            Debug.Assert(_rentedBuffer.Length >= _index);

            oldBuffer.AsSpan(0, _index).CopyTo(_rentedBuffer);

            ArrayPool<T>.Shared.ReturnAndClear(oldBuffer);
        }

        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
    }

    private static ObjectDisposedException Disposed() => new(nameof(RentedBufferWriter<T>));
}