using IT.Buffers.Extensions;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public sealed class RentedBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    public static BufferPool<RentedBufferWriter<T>> Pool
        => BufferPool<RentedBufferWriter<T>>.Shared;

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
        BufferSize.CheckAndResizeBuffer(ref _buffer, _written, sizeHint);
        return _buffer.AsMemory(_written);
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        BufferSize.CheckAndResizeBuffer(ref _buffer, _written, sizeHint);
        return _buffer.AsSpan(_written);
    }
}