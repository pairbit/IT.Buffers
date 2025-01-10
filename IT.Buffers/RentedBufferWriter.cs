using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IT.Buffers;

public sealed class RentedBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    public const int MinimumBufferSize = 256;
    public const int MaximumBufferSize = 0X7FFFFFC7;

    private T[]? _rentedBuffer;
    private int _index;

#if NET
    static RentedBufferWriter()
    {
        Debug.Assert(MaximumBufferSize == Array.MaxLength);
    }
#endif

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public RentedBufferWriter(int initialCapacity = 0)
    {
        if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        if (initialCapacity == 0) initialCapacity = MinimumBufferSize;

        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            Debug.Assert(_index <= _rentedBuffer.Length);

            return _rentedBuffer.AsMemory(0, _index);
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            Debug.Assert(_index <= _rentedBuffer.Length);

            return _rentedBuffer.AsSpan(0, _index);
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public int WrittenCount
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            return _index;
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public int Capacity
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            return _rentedBuffer.Length;
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public int FreeCapacity
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            return _rentedBuffer.Length - _index;
        }
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void Clear()
    {
        var rentedBuffer = _rentedBuffer ?? throw Disposed();

        Debug.Assert(rentedBuffer != null);
        Debug.Assert(_index <= rentedBuffer.Length);

        rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void ResetWrittenCount()
    {
        if (_rentedBuffer == null) throw Disposed();

        _index = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void Return()
    {
        var rentedBuffer = _rentedBuffer ?? throw Disposed();
        _rentedBuffer = null;
        ArrayPool<T>.Shared.Return(rentedBuffer, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }

    public void Dispose()
    {
        var rentedBuffer = _rentedBuffer;
        if (rentedBuffer == null) return;

        _rentedBuffer = null;
        ArrayPool<T>.Shared.Return(rentedBuffer, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Initialize(int initialCapacity = 0)
    {
        if (_rentedBuffer != null) throw new InvalidOperationException("not return");
        if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        if (initialCapacity == 0) initialCapacity = MinimumBufferSize;

        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    public void Advance(int count)
    {
        if (_rentedBuffer == null) throw Disposed();

        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

        if (_index > _rentedBuffer.Length - count)
            throw new ArgumentOutOfRangeException(nameof(count));

        _index += count;
    }

    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsSpan(_index);
    }

    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OutOfMemoryException"></exception>
    private void CheckAndResizeBuffer(int sizeHint)
    {
        if (_rentedBuffer == null) throw Disposed();

        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        if (sizeHint == 0) sizeHint = MinimumBufferSize;

        int currentLength = _rentedBuffer.Length;
        int availableSpace = currentLength - _index;

        // If we've reached ~1GB written, grow to the maximum buffer
        // length to avoid incessant minimal growths causing perf issues.
        if (_index >= MaximumBufferSize / 2)
        {
            sizeHint = Math.Max(sizeHint, MaximumBufferSize - currentLength);
        }

        if (sizeHint > availableSpace)
        {
            int growBy = Math.Max(sizeHint, currentLength);

            int newSize = currentLength + growBy;

            if ((uint)newSize > MaximumBufferSize)
            {
                newSize = currentLength + sizeHint;
                if ((uint)newSize > MaximumBufferSize)
                {
                    throw new OutOfMemoryException($"Size {(uint)newSize} > {MaximumBufferSize}");
                }
            }

            T[] oldBuffer = _rentedBuffer;

            _rentedBuffer = ArrayPool<T>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _index);
            Debug.Assert(_rentedBuffer.Length >= _index);

            oldBuffer.AsSpan(0, _index).CopyTo(_rentedBuffer);

            ArrayPool<T>.Shared.Return(oldBuffer, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }

        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
    }

    private static ObjectDisposedException Disposed() => new(nameof(RentedBufferWriter<T>));
}