using IT.Buffers.Extensions;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public sealed class RentedBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private T[]? _buffer;
    private int _written;

    public RentedBufferWriter()
    {
        _buffer = ArrayPool<T>.Shared.Rent(BufferSize.Min);
        _written = 0;
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public RentedBufferWriter(int capacity)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (capacity == 0) capacity = BufferSize.Min;

        _buffer = ArrayPool<T>.Shared.Rent(capacity);
        _written = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            if (_buffer == null) throw Disposed();

            Debug.Assert(_written <= _buffer.Length);

            return _buffer.AsMemory(0, _written);
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            if (_buffer == null) throw Disposed();

            Debug.Assert(_written <= _buffer.Length);

            return _buffer.AsSpan(0, _written);
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public int WrittenCount
    {
        get
        {
            if (_buffer == null) throw Disposed();

            return _written;
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public int Capacity
    {
        get
        {
            if (_buffer == null) throw Disposed();

            return _buffer.Length;
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public int FreeCapacity
    {
        get
        {
            if (_buffer == null) throw Disposed();

            return _buffer.Length - _written;
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void Clear()
    {
        var buffer = _buffer ?? throw Disposed();

        Debug.Assert(buffer != null);
        Debug.Assert(_written <= buffer.Length);

        buffer.AsSpan(0, _written).Clear();
        _written = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void ResetWrittenCount()
    {
        if (_buffer == null) throw Disposed();

        _written = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void Return()
    {
        var buffer = _buffer ?? throw Disposed();
        _buffer = null;
        ArrayPool<T>.Shared.ReturnAndClear(buffer);
    }

    public void Dispose()
    {
        var buffer = _buffer;
        if (buffer == null) return;

        _buffer = null;
        ArrayPool<T>.Shared.ReturnAndClear(buffer);
    }

    /// <exception cref="InvalidOperationException"></exception>
    public void Initialize()
    {
        if (_buffer != null) throw new InvalidOperationException("buffer not returned");

        _buffer = ArrayPool<T>.Shared.Rent(BufferSize.Min);
        _written = 0;
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Initialize(int capacity)
    {
        if (_buffer != null) throw new InvalidOperationException("buffer not returned");
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (capacity == 0) capacity = BufferSize.Min;

        _buffer = ArrayPool<T>.Shared.Rent(capacity);
        _written = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void Advance(int count)
    {
        if (_buffer == null) throw Disposed();

        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

        var written = _written + count;
        if (written > _buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _written = written;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsMemory(_written);
    }

    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsSpan(_written);
    }

    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    private void CheckAndResizeBuffer(int sizeHint)
    {
        if (_buffer == null) throw Disposed();

        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        if (sizeHint == 0) sizeHint = BufferSize.Min;

        int currentLength = _buffer.Length;
        int availableSpace = currentLength - _written;

        // If we've reached ~1GB written, grow to the maximum buffer
        // length to avoid incessant minimal growths causing perf issues.
        if (_written >= BufferSize.MaxHalf)
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

            T[] oldBuffer = _buffer;

            _buffer = ArrayPool<T>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _written);
            Debug.Assert(_buffer.Length >= _written);

            oldBuffer.AsSpan(0, _written).CopyTo(_buffer);

            ArrayPool<T>.Shared.ReturnAndClear(oldBuffer);
        }

        Debug.Assert(_buffer.Length - _written > 0);
        Debug.Assert(_buffer.Length - _written >= sizeHint);
    }

    private static ObjectDisposedException Disposed() => new(nameof(RentedBufferWriter<T>));
}