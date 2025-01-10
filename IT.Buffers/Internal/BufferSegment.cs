using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace IT.Buffers.Internal;

internal struct BufferSegment
{
    private byte[] _buffer;
    private int _written;

    public readonly bool IsNull => _buffer == null;

    public readonly Memory<byte> WrittenMemory => _buffer.AsMemory(0, _written);

    public readonly Span<byte> WrittenSpan => _buffer.AsSpan(0, _written);

    public readonly int WrittenCount => _written;

    public readonly Span<byte> FreeSpan => _buffer.AsSpan(_written);

    public BufferSegment(int size)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(size);
        _written = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        _written += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (_buffer != null)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }
        _buffer = null!;
        _written = 0;
    }
}