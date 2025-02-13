using IT.Buffers.Extensions;
using IT.Buffers.Interfaces;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public sealed class RentedBufferWriter<T> : IAdvancedBufferWriter<T>, IDisposable
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

    public Memory<T> WrittenMemory
    {
        get
        {
            Debug.Assert(_buffer.Length >= _written);

            return _buffer.AsMemory(0, _written);
        }
    }

    public Span<T> WrittenSpan
    {
        get
        {
            Debug.Assert(_buffer.Length >= _written);

            return _buffer.AsSpan(0, _written);
        }
    }

    public int Written => _written;

    long IAdvancedBufferWriter<T>.WrittenLong => _written;

    int IAdvancedBufferWriter<T>.Segments => 1;

    bool IAdvancedBufferWriter<T>.HasMemory => true;

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

    public void Reset()
    {
        var buffer = _buffer;
        if (buffer.Length > 0)
        {
            _buffer = [];
            _written = 0;
            BufferPool.Return(buffer);
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

    public bool TryWrite(Span<T> span)
    {
        var written = _written;
        if (span.Length < written) return false;

        if (written > 0)
        {
            Debug.Assert(_buffer.Length >= written);

            _buffer.AsSpan(0, written).CopyTo(span);
        }

        return true;
    }

    public void Write<TBufferWriter>(ref TBufferWriter writer) where TBufferWriter : IBufferWriter<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var written = _written;
        if (written > 0)
        {
            Debug.Assert(_buffer.Length >= written);

            RefBufferWriter.WriteSpan(ref writer, new ReadOnlySpan<T>(_buffer, 0, written));
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public T[] DangerousGetBuffer() => _buffer;

    Memory<T> IAdvancedBufferWriter<T>.GetWrittenMemory(int segment)
    {
        if (segment != 0) throw new ArgumentOutOfRangeException(nameof(segment));
        return WrittenMemory;
    }

    void IDisposable.Dispose() => Reset();
}