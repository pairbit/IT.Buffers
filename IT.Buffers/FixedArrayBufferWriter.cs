using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace IT.Buffers;

public struct FixedArrayBufferWriter<T> : IBufferWriter<T>
{
    private readonly T[] _buffer;
    private int _written;

    public FixedArrayBufferWriter(T[] buffer)
    {
        _buffer = buffer;
        _written = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        _written += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        var memory = _buffer.AsMemory(_written);
        if (memory.Length >= sizeHint) return memory;

        throw new InvalidOperationException("invalid sizeHint");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan(int sizeHint = 0)
    {
        var span = _buffer.AsSpan(_written);
        if (span.Length >= sizeHint) return span;

        throw new InvalidOperationException("invalid sizeHint");
    }

    public T[] GetFilledBuffer()
    {
        if (_written != _buffer.Length) throw new InvalidOperationException("Not filled buffer");

        return _buffer;
    }
}