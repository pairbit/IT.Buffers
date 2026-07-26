using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public enum RentedArrayType : byte
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
    External = 3
}

public readonly struct Buffer<T>
{
    public static Buffer<T> Empty { get; } = new([]);

    private readonly object? _buffer;
    private readonly int _offset;
    private readonly int _count;

    internal BufferType Type
    {
        get
        {
            var buffer = _buffer;
            if (buffer is null) return BufferType.Null;
            if (buffer is T[]) return BufferType.Array;
            if (buffer is MemoryManager<T>) return BufferType.MemoryManager;
            if (buffer is IMemoryOwner<T>) return BufferType.MemoryOwner;

            return BufferType.Unknown;
        }
    }

    public RentedArrayType RentedArrayType
    {
        get
        {
            if (_buffer is T[])
            {
                if (_offset < 0) return _count < 0 ? RentedArrayType.External : RentedArrayType.Global;

                if (_count < 0) return RentedArrayType.Shared;
            }
            return RentedArrayType.None;
        }
    }

    public T[]? Array => _buffer as T[];

    public MemoryManager<T>? MemoryManager => _buffer as MemoryManager<T>;

    public IMemoryOwner<T>? MemoryOwner => _buffer as IMemoryOwner<T>;

    public Memory<T> Memory
    {
        get
        {
            var count = Count;
            if (count == 0) return default;

            var buffer = _buffer;
            if (buffer is T[] array)
                return new(array, Offset, count);

            if (buffer is MemoryManager<T> memoryManager)
                return memoryManager.Memory.Slice(Offset, count);

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

            if (buffer is MemoryManager<T> memoryManager)
                return memoryManager.GetSpan().Slice(Offset, count);

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

    private Buffer(object buffer, int offset, int count)
    {
        _buffer = buffer;
        _offset = offset;
        _count = count;
    }

    public Buffer(T[] array)
    {
        _buffer = array ?? throw new ArgumentNullException(nameof(array));
        _offset = 0;
        _count = array.Length;
    }

    public Buffer(T[] array, RentedArrayType type)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        if (type == RentedArrayType.Shared)
        {
            _buffer = array;
            _offset = 0;
            _count = ~array.Length;
        }
        else if (type == RentedArrayType.Global)
        {
            _buffer = array;
            _offset = ~0;
            _count = array.Length;
        }
        else if (type == RentedArrayType.External)
        {
            _buffer = array;
            _offset = ~0;
            _count = ~array.Length;
        }
        else if (type == RentedArrayType.None)
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

    public Buffer(T[] array, int offset, int count, RentedArrayType type)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        if ((uint)offset > (uint)array.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((uint)count > (uint)(array.Length - offset))
            throw new ArgumentOutOfRangeException(nameof(count));

        _buffer = array;
        _offset = offset;
        _count = count;

        if (type == RentedArrayType.Shared)
        {
            _offset = offset;
            _count = ~count;
        }
        else if (type == RentedArrayType.Global)
        {
            _offset = ~offset;
            _count = count;
        }
        else if (type == RentedArrayType.External)
        {
            _offset = ~offset;
            _count = ~count;
        }
        else if (type == RentedArrayType.None)
        {
            _offset = offset;
            _count = count;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public Buffer(MemoryManager<T> memoryManager)
    {
        _buffer = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
        _offset = 0;
        _count = memoryManager.Memory.Length;
    }

    public Buffer(MemoryManager<T> memoryManager, int offset, int count)
    {
        if (memoryManager == null) throw new ArgumentNullException(nameof(memoryManager));
        var length = memoryManager.Memory.Length;

        if ((uint)offset > (uint)length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((uint)count > (uint)(length - offset))
            throw new ArgumentOutOfRangeException(nameof(count));

        _buffer = memoryManager;
        _offset = offset;
        _count = count;
    }

    public Buffer(Memory<T> memory)
    {
        if (memory.IsEmpty)
        {
            this = Empty;
        }
        else if (MemoryMarshal.TryGetArray((ReadOnlyMemory<T>)memory, out var segment))
        {
            _buffer = segment.Array;
            _offset = segment.Offset;
            _count = segment.Count;
        }
        else if (MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out var manager, out var start, out var length))
        {
            _buffer = manager;
            _offset = start;
            _count = length;
        }
        throw new ArgumentException("Unrecognized memory type", nameof(memory));
    }

    public Buffer(IMemoryOwner<T> memoryOwner)
    {
        _buffer = memoryOwner ?? throw new ArgumentNullException(nameof(memoryOwner));
        _offset = 0;
        _count = memoryOwner.Memory.Length;
    }

    public Buffer(IMemoryOwner<T> memoryOwner, int offset, int count)
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
        => obj is Buffer<T> other && Equals(other);

    public bool Equals(Buffer<T> other)
        => other._buffer == _buffer && other._offset == _offset && other._count == _count;

    public Buffer<T> Slice(int index)
    {
        var count = Count;
        if ((uint)index > (uint)count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Offset + index, count - index, RentedArrayType);

        if (buffer is MemoryManager<T> memoryManager)
            return new(memoryManager, Offset + index, count - index);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return new(memoryOwner, Offset + index, count - index);

        throw InvalidState();
    }

    public Buffer<T> Slice(int index, int count)
    {
        var oldCount = Count;
        if ((uint)index > (uint)oldCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        if ((uint)count > (uint)(oldCount - index))
            throw new ArgumentOutOfRangeException(nameof(count));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Offset + index, count, RentedArrayType);

        if (buffer is MemoryManager<T> memoryManager)
            return new(memoryManager, Offset + index, count);

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

        if (buffer is MemoryManager<T> memoryManager)
            return memoryManager.GetSpan().Slice(Offset, count).ToArray();

        if (buffer is IMemoryOwner<T> memoryOwner)
            return memoryOwner.Memory.Slice(Offset, count).ToArray();

        throw InvalidState();
    }

    private static InvalidOperationException InvalidState()
        => new("_array == null");

    public static bool operator ==(Buffer<T> left, Buffer<T> right) => left.Equals(right);

    public static bool operator !=(Buffer<T> left, Buffer<T> right) => !left.Equals(right);

    public static implicit operator Buffer<T>(T[] array) => array != null ? new(array) : default;

    public static implicit operator Memory<T>(Buffer<T> value) => value.Memory;

    public static implicit operator Span<T>(Buffer<T> value) => value.Span;

    internal enum BufferType
    {
        Null,
        Array,
        MemoryManager,
        MemoryOwner,
        Unknown
    }
}