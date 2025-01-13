using IT.Buffers.Extensions;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public struct ValueRentedBufferWriter<T> : IAdvancedBufferWriter<T>, IDisposable
{
    private T[]? _buffer;
    private int _written;

    public readonly ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            var buffer = _buffer;
            if (buffer == null) return default;

            Debug.Assert(buffer.Length >= _written);

            return buffer.AsMemory(0, _written);
        }
    }

    public readonly ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            var buffer = _buffer;
            if (buffer == null) return default;

            Debug.Assert(buffer.Length >= _written);

            return buffer.AsSpan(0, _written);
        }
    }

    public readonly int Written => _written;

    readonly long IAdvancedBufferWriter<T>.WrittenLong => _written;

    public readonly int Capacity
    {
        get
        {
            var buffer = _buffer;
            return buffer == null ? 0 : buffer.Length;
        }
    }

    public readonly int FreeCapacity
    {
        get
        {
            var buffer = _buffer;
            if (buffer == null) return 0;

            Debug.Assert(buffer.Length >= _written);

            return buffer.Length - _written;
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
        if (buffer != null)
        {
            _buffer = null;
            _written = 0;
            ArrayPool<T>.Shared.ReturnAndClear(buffer);
        }
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Advance(int count)
    {
        var buffer = _buffer;
        if (buffer == null || count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

        var written = _written + count;
        if (written > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _written = written;
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        if (_buffer == null) return _buffer = BufferSize.RentBuffer<T>(sizeHint);

        BufferSize.CheckAndResizeBuffer(ref _buffer, _written, sizeHint);
        return _buffer.AsMemory(_written);
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        if (_buffer == null) return _buffer = BufferSize.RentBuffer<T>(sizeHint);

        BufferSize.CheckAndResizeBuffer(ref _buffer, _written, sizeHint);
        return _buffer.AsSpan(_written);
    }
}