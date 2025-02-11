using IT.Buffers.Extensions;
using IT.Buffers.Interfaces;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IT.Buffers;

public ref struct ValueFixedSpanBufferWriter<T> : IAdvancedBufferWriter<T>
{
    private readonly Span<T> _buffer;
    private int _written;

    public ValueFixedSpanBufferWriter(Span<T> buffer)
    {
        _buffer = buffer;
        _written = 0;
    }

    public readonly Span<T> WrittenSpan
    {
        get
        {
            Debug.Assert(_buffer.Length >= _written);
            return _buffer.Slice(0, _written);
        }
    }

    public readonly int Written => _written;

    readonly long IAdvancedBufferWriter<T>.WrittenLong => _written;

    readonly int IAdvancedBufferWriter<T>.Segments => 1;

    public readonly int Capacity => _buffer.Length;

    public readonly int FreeCapacity
    {
        get
        {
            Debug.Assert(_buffer.Length >= _written);
            return _buffer.Length - _written;
        }
    }

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

        var written = _written + count;
        if (written > _buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _written = written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Memory<T> GetMemory(int sizeHint = 0)
        => throw new NotSupportedException($"Method '{nameof(GetMemory)}' is not supported");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> GetSpan(int sizeHint = 0)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        var span = _buffer.Slice(_written);
        if (span.Length >= sizeHint) return span;

        throw new ArgumentOutOfRangeException(nameof(sizeHint));
    }

    public void ResetWritten() => _written = 0;

    public readonly bool TryWrite(Span<T> span)
    {
        var written = _written;
        if (span.Length < written) return false;

        if (written > 0)
        {
            Debug.Assert(_buffer.Length >= written);
            _buffer.Slice(0, written).CopyTo(span);
        }

        return true;
    }

    public readonly void Write<TBufferWriter>(ref TBufferWriter writer) where TBufferWriter : IBufferWriter<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var written = _written;
        if (written > 0)
        {
            Debug.Assert(_buffer.Length >= written);
            xBufferWriter.WriteSpan(ref writer, (ReadOnlySpan<T>)_buffer.Slice(0, _written));
        }
    }

    readonly Memory<T> IAdvancedBufferWriter<T>.GetWrittenMemory(int segment)
        => throw new NotSupportedException($"Method '{nameof(IAdvancedBufferWriter<T>.GetWrittenMemory)}' is not supported");
}