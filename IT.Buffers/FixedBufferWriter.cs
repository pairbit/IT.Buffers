using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IT.Buffers;

public struct FixedBufferWriter<T> : IBufferWriter<T>
{
    private readonly T[] _buffer;
    private int _written;

    public FixedBufferWriter(T[] buffer)
    {
        _buffer = buffer;
        _written = 0;
    }

    public readonly ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _written);

    public readonly ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _written);

    public readonly int WrittenCount => _written;

    public readonly int Capacity => _buffer.Length;

    public readonly int FreeCapacity => _buffer.Length - _written;

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
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        if (sizeHint == 0) sizeHint = 1;

        var memory = _buffer.AsMemory(_written);
        if (memory.Length >= sizeHint) return memory;

        throw new InvalidOperationException("invalid sizeHint");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> GetSpan(int sizeHint = 0)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        if (sizeHint == 0) sizeHint = 1;

        var span = _buffer.AsSpan(_written);
        if (span.Length >= sizeHint) return span;

        throw new InvalidOperationException("invalid sizeHint");
    }

    public void Clear()
    {
        Debug.Assert(_buffer.Length >= _written);

        _buffer.AsSpan(0, _written).Clear();
        _written = 0;
    }

    public void ResetWrittenCount() => _written = 0;

    public readonly T[] GetFilledBuffer()
    {
        if (_written != _buffer.Length) throw new InvalidOperationException("Not filled buffer");

        return _buffer;
    }
}