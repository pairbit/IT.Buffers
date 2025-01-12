using IT.Buffers.Extensions;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public sealed class RentedBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private T[] _buffer;
    private int _written;

    public RentedBufferWriter()
    {
        _buffer = [];
        _written = 0;
    }

    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            Debug.Assert(_buffer.Length >= _written);

            return _buffer.AsMemory(0, _written);
        }
    }

    public ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            Debug.Assert(_buffer.Length >= _written);

            return _buffer.AsSpan(0, _written);
        }
    }

    public int Written => _written;

    public int Capacity => _buffer.Length;

    public int FreeCapacity
    {
        get
        {
            Debug.Assert(_buffer.Length >= _written);

            return _buffer.Length - _written;
        }
    }

    //public void Clear()
    //{
    //    Debug.Assert(_buffer.Length >= _written);

    //    _buffer.AsSpan(0, _written).Clear();
    //    _written = 0;
    //}

    public void ResetWritten()
    {
        _written = 0;
    }

    public void Dispose()
    {
        var buffer = _buffer;
        if (buffer.Length > 0)
        {
            _buffer = [];
            _written = 0;
            ArrayPool<T>.Shared.ReturnAndClear(buffer);
        }
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Advance(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

        var written = _written + count;
        if (written > _buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _written = written;
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsMemory(_written);
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsSpan(_written);
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    private void CheckAndResizeBuffer(int sizeHint)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        if (sizeHint == 0) sizeHint = 1;

        var buffer = _buffer;
        var written = _written;
        int capacity = buffer.Length;

        Debug.Assert(capacity >= written);

        int freeCapacity = capacity - written;

        // If we've reached ~1GB written, grow to the maximum buffer
        // length to avoid incessant minimal growths causing perf issues.
        if (written >= BufferSize.MaxHalf)
        {
            sizeHint = Math.Max(sizeHint, BufferSize.Max - capacity);
        }

        if (sizeHint > freeCapacity)
        {
            int growBy = Math.Max(sizeHint, capacity);

            int newSize = capacity + growBy;

            if ((uint)newSize > BufferSize.Max)
            {
                newSize = capacity + sizeHint;
                if ((uint)newSize > BufferSize.Max)
                {
                    throw new OutOfMemoryException($"Size {(uint)newSize} > {BufferSize.Max}");
                }
            }

            if (capacity == 0)
            {
                Debug.Assert(written == 0);

                buffer = _buffer = ArrayPool<T>.Shared.Rent(newSize);

                Debug.Assert(buffer.Length >= written);
            }
            else
            {
                T[] oldBuffer = buffer;

                buffer = _buffer = ArrayPool<T>.Shared.Rent(newSize);

                Debug.Assert(buffer.Length >= written);

                if (written > 0)
                    oldBuffer.AsSpan(0, written).CopyTo(buffer);

                ArrayPool<T>.Shared.ReturnAndClear(oldBuffer);
            }
        }

        Debug.Assert(buffer.Length - written > 0);
        Debug.Assert(buffer.Length - written >= sizeHint);
    }
}