using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public sealed class RentedBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    public const int MinimumBufferSize = 256;
    public const int MaximumBufferSize = 0X7FFFFFC7;

    private T[]? _rentedBuffer;
    private int _index;

#if NET && DEBUG
    static RentedBufferWriter()
    {
        Debug.Assert(MaximumBufferSize == Array.MaxLength);
    }
#endif

    public RentedBufferWriter()
    {
        _rentedBuffer = ArrayPool<T>.Shared.Rent(MinimumBufferSize);
        _index = 0;
    }

    public RentedBufferWriter(int initialCapacity)
    {
        if (initialCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            Debug.Assert(_index <= _rentedBuffer.Length);

            return _rentedBuffer.AsMemory(0, _index);
        }
    }

    public int WrittenCount
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            return _index;
        }
    }

    public int Capacity
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            return _rentedBuffer.Length;
        }
    }

    public int FreeCapacity
    {
        get
        {
            if (_rentedBuffer == null) throw Disposed();

            return _rentedBuffer.Length - _index;
        }
    }

    public void Clear()
    {
        if (_rentedBuffer == null) throw Disposed();

        ClearHelper();
    }

    public void ResetWrittenCount()
    {
        if (_rentedBuffer == null) throw Disposed();

        _index = 0;
    }

    public void ClearAndReturn()
    {
        if (_rentedBuffer == null) throw Disposed();

        ClearHelper();
        T[] rentedBuffer = _rentedBuffer;
        _rentedBuffer = null;
        ArrayPool<T>.Shared.Return(rentedBuffer);//clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>()
    }

    public void Dispose()
    {
        if (_rentedBuffer == null) return;

        ClearHelper();
        T[] rentedBuffer = _rentedBuffer;
        _rentedBuffer = null;
        ArrayPool<T>.Shared.Return(rentedBuffer);
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Initialize(int initialCapacity)
    {
        if (_rentedBuffer != null) throw new InvalidOperationException("not return");
        if (initialCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    public void Advance(int count)
    {
        if (_rentedBuffer == null) throw Disposed();

        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

        if (_index > _rentedBuffer.Length - count)
            throw new ArgumentOutOfRangeException(nameof(count));

        _index += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsSpan(_index);
    }

    private void ClearHelper()
    {
#if DEBUG
        Debug.Assert(_rentedBuffer != null);
        Debug.Assert(_index <= _rentedBuffer.Length);
#endif
        _rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

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
                    //ThrowHelper.ThrowOutOfMemoryException_BufferMaximumSizeExceeded((uint)newSize);
                }
            }

            T[] oldBuffer = _rentedBuffer;

            _rentedBuffer = ArrayPool<T>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _index);
            Debug.Assert(_rentedBuffer.Length >= _index);

            Span<T> oldBufferAsSpan = oldBuffer.AsSpan(0, _index);
            oldBufferAsSpan.CopyTo(_rentedBuffer);
            oldBufferAsSpan.Clear();
            ArrayPool<T>.Shared.Return(oldBuffer);
        }
#if DEBUG
        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
#endif
    }

    private static ObjectDisposedException Disposed() => new(nameof(RentedBufferWriter<T>));
}