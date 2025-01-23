using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace IT.Buffers.Internal;

internal struct BufferSegment<T>
{
    private T[] _buffer;
    private int _written;

    public readonly bool IsNull => _buffer == null;

    public readonly Memory<T> WrittenMemory => _buffer.AsMemory(0, _written);

    public readonly ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _written);

    public readonly int Written => _written;

    public readonly int Capacity => _buffer.Length;

    public readonly Memory<T> FreeMemory => _buffer.AsMemory(_written);

    public readonly Span<T> FreeSpan => _buffer.AsSpan(_written);

    public BufferSegment(int size)
    {
        _buffer = ArrayPool<T>.Shared.Rent(size);
        _written = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        var written = _written + count;
        if (written > _buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _written = written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        var buffer = _buffer;
        if (buffer != null)
        {
            _buffer = null!;
            _written = 0;
            ArrayPoolShared.ReturnAndClear(buffer);
        }
    }
}