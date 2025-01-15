using IT.Buffers.Extensions;
using IT.Buffers.Interfaces;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IT.Buffers;

public struct ValueFixedBufferWriter<T> : IAdvancedBufferWriter<T>
{
    private readonly T[]? _buffer;
    private int _written;

    public ValueFixedBufferWriter(T[] buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

        _buffer = buffer;
        _written = 0;
    }

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

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        var buffer = _buffer;
        if (buffer == null || count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

        var written = _written + count;
        if (written > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _written = written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Memory<T> GetMemory(int sizeHint = 0)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        var buffer = _buffer ?? throw new InvalidOperationException("buffer is empty");
        if (sizeHint == 0) sizeHint = 1;

        var memory = buffer.AsMemory(_written);
        if (memory.Length >= sizeHint) return memory;

        throw new ArgumentOutOfRangeException(nameof(sizeHint));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> GetSpan(int sizeHint = 0)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        var buffer = _buffer ?? throw new InvalidOperationException("buffer is empty");
        if (sizeHint == 0) sizeHint = 1;

        var span = buffer.AsSpan(_written);
        if (span.Length >= sizeHint) return span;

        throw new ArgumentOutOfRangeException(nameof(sizeHint));
    }

    public void ResetWritten() => _written = 0;

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
    {
        var buffer = _buffer;
        var written = _written;
        if (buffer != null && written > 0)
        {
            Debug.Assert(buffer.Length >= written);

            xBufferWriter.WriteSpan(ref writer, new ReadOnlySpan<T>(buffer, 0, written));
        }
    }

    readonly Memory<T> IAdvancedBufferWriter<T>.GetWrittenMemory(int segment)
    {
        if (segment != 0) throw new ArgumentOutOfRangeException(nameof(segment));
        return WrittenMemory;
    }
}