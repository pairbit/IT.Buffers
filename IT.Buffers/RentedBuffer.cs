using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace IT.Buffers;

public enum RentedBufferType : byte
{
    /// <summary>
    /// Not rented
    /// </summary>
    None = 0,

    /// <summary>
    /// Rented from an ArrayPool.Shared
    /// </summary>
    Shared = 1,

    /// <summary>
    /// 
    /// </summary>
    Global = 2,

    /// <summary>
    /// Rented from an external pool
    /// </summary>
    External = 3,

    MemoryOwner = 4
}

//Buffer?
public readonly struct RentedBuffer<T>
{
    public static RentedBuffer<T> Empty { get; } = new([]);

    private readonly object? _buffer;
    private readonly int _offset;
    private readonly int _count;

    public RentedBufferType Type
    {
        get
        {
            if (_buffer is IMemoryOwner<T>) return RentedBufferType.MemoryOwner;

            if (_offset < 0) return _count < 0 ? RentedBufferType.External : RentedBufferType.Global;

            if (_count < 0) return RentedBufferType.Shared;

            return RentedBufferType.None;
        }
    }

    public T[]? Array => _buffer as T[];

    public IMemoryOwner<T> MemoryOwner => (IMemoryOwner<T>)_buffer!;

    public Memory<T> Memory
    {
        get
        {
            var count = Count;
            if (count == 0) return default;

            var buffer = _buffer;
            if (buffer is T[] array)
                return new(array, Offset, count);

            if (buffer is IMemoryOwner<T> memoryOwner)
                return memoryOwner.Memory.Slice(Offset, count);

            throw InvalidState();
        }
    }
    
    public Span<T> Span
    {
        get
        {
            var count = Count;
            if (count == 0) return default;

            var buffer = _buffer;
            if (buffer is T[] array)
                return new(array, Offset, count);

            if (buffer is IMemoryOwner<T> memoryOwner)
                return memoryOwner.Memory.Span.Slice(Offset, count);

            throw InvalidState();
        }
    }

    public int Offset => _offset < 0 ? ~_offset : _offset;

    public int Count => _count < 0 ? ~_count : _count;

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return Span[Offset + index];
        }
        set
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            Span[Offset + index] = value;
        }
    }

    private RentedBuffer(object buffer, int offset, int count)
    {
        _buffer = buffer;
        _offset = offset;
        _count = count;
    }

    public RentedBuffer(T[] array)
    {
        _buffer = array ?? throw new ArgumentNullException(nameof(array));
        _offset = 0;
        _count = array.Length;
    }

    public RentedBuffer(T[] array, RentedBufferType type)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        if (type == RentedBufferType.Shared)
        {
            _buffer = array;
            _offset = 0;
            _count = ~array.Length;
        }
        else if (type == RentedBufferType.Global)
        {
            _buffer = array;
            _offset = ~0;
            _count = array.Length;
        }
        else if (type == RentedBufferType.External)
        {
            _buffer = array;
            _offset = ~0;
            _count = ~array.Length;
        }
        else if (type == RentedBufferType.None)
        {
            _buffer = array;
            _offset = 0;
            _count = array.Length;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public RentedBuffer(T[] array, int offset, int count, RentedBufferType type)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        if ((uint)offset > (uint)array.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((uint)count > (uint)(array.Length - offset))
            throw new ArgumentOutOfRangeException(nameof(count));

        _buffer = array;
        _offset = offset;
        _count = count;

        if (type == RentedBufferType.Shared)
        {
            _offset = offset;
            _count = ~count;
        }
        else if (type == RentedBufferType.Global)
        {
            _offset = ~offset;
            _count = count;
        }
        else if (type == RentedBufferType.External)
        {
            _offset = ~offset;
            _count = ~count;
        }
        else if (type == RentedBufferType.None)
        {
            _offset = offset;
            _count = count;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public RentedBuffer(IMemoryOwner<T> memoryOwner)
    {
        _buffer = memoryOwner ?? throw new ArgumentNullException(nameof(memoryOwner));
        _offset = 0;
        _count = memoryOwner.Memory.Length;
    }

    public RentedBuffer(IMemoryOwner<T> memoryOwner, int offset, int count)
    {
        if (memoryOwner == null) throw new ArgumentNullException(nameof(memoryOwner));
        var length = memoryOwner.Memory.Length;

        if ((uint)offset > (uint)length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((uint)count > (uint)(length - offset))
            throw new ArgumentOutOfRangeException(nameof(count));

        _buffer = memoryOwner;
        _offset = offset;
        _count = count;
    }

    public override int GetHashCode()
        => _buffer is null ? 0 : HashCode.Combine(_offset, _count, _buffer.GetHashCode());

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is RentedBuffer<T> other && Equals(other);

    public bool Equals(RentedBuffer<T> other)
        => other._buffer == _buffer && other._offset == _offset && other._count == _count;

    public RentedBuffer<T> Slice(int index)
    {
        var count = Count;
        if ((uint)index > (uint)count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Offset + index, count - index, Type);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return new(memoryOwner, Offset + index, count - index);
        
        throw InvalidState();
    }

    public RentedBuffer<T> Slice(int index, int count)
    {
        var oldCount = Count;
        if ((uint)index > (uint)oldCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        if ((uint)count > (uint)(oldCount - index))
            throw new ArgumentOutOfRangeException(nameof(count));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Offset + index, count, Type);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return new(memoryOwner, Offset + index, count);

        throw InvalidState();
    }

    public T[] ToArray()
    {
        var count = Count;
        if (count == 0) return [];

        var buffer = _buffer;
        if (buffer is T[] array)
        {
            var copy = new T[count];

            System.Array.Copy(array, Offset, copy, 0, count);

            return copy;
        }

        if (buffer is IMemoryOwner<T> memoryOwner)
        {
            return memoryOwner.Memory.Slice(Offset, count).ToArray();
        }

        throw InvalidState();
    }

    private static InvalidOperationException InvalidState()
        => new("_array == null");

    public static bool operator ==(RentedBuffer<T> left, RentedBuffer<T> right) => left.Equals(right);

    public static bool operator !=(RentedBuffer<T> left, RentedBuffer<T> right) => !left.Equals(right);

    public static implicit operator RentedBuffer<T>(T[] array) => array != null ? new(array) : default;

    public static implicit operator Memory<T>(RentedBuffer<T> value) => value.Memory;

    public static implicit operator Span<T>(RentedBuffer<T> value) => value.Span;
}