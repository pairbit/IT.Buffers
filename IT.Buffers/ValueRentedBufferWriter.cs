using IT.Buffers.Extensions;
using IT.Buffers.Interfaces;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public struct ValueRentedBufferWriter<T> : IAdvancedBufferWriter<T>
{
    private T[]? _buffer;
    private int _written;

    public readonly Memory<T> WrittenMemory
    {
        get
        {
            var buffer = _buffer;
            if (buffer == null) return default;

            Debug.Assert(buffer.Length >= _written);

            return buffer.AsMemory(0, _written);
        }
    }

    public readonly Span<T> WrittenSpan
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

    readonly int IAdvancedBufferWriter<T>.Segments => 1;

    readonly bool IAdvancedBufferWriter<T>.HasMemory => true;

    readonly bool IAdvancedBufferWriter<T>.IsFixed => false;

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

    public void ResetWritten()
    {
        _written = 0;
    }

    public void Reset()
    {
        var buffer = _buffer;
        if (buffer != null)
        {
            _buffer = null;
            _written = 0;
            BufferPool.Return(buffer);
        }
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Advance(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count > 0)
        {
            var buffer = _buffer ?? throw new ArgumentOutOfRangeException(nameof(count));
            var written = _written + count;
            if (written > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

            _written = written;
        }
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

    public readonly bool TryWrite(Span<T> span)
    {
        var written = _written;
        if (span.Length < written) return false;

        var buffer = _buffer;
        if (buffer != null && written > 0)
        {
            Debug.Assert(buffer.Length >= written);

            buffer.AsSpan(0, written).CopyTo(span);
        }

        return true;
    }

    public readonly void Write<TBufferWriter>(ref TBufferWriter writer) where TBufferWriter : IBufferWriter<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var buffer = _buffer;
        var written = _written;
        if (buffer != null && written > 0)
        {
            Debug.Assert(buffer.Length >= written);

            RefBufferWriter.WriteSpan(ref writer, new ReadOnlySpan<T>(buffer, 0, written));
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly T[]? DangerousGetBuffer() => _buffer;

    readonly Memory<T> IAdvancedBufferWriter<T>.GetWrittenMemory(int segment)
    {
        if (segment != 0) throw new ArgumentOutOfRangeException(nameof(segment));
        return WrittenMemory;
    }
}