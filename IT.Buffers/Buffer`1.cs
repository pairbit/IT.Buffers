using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public readonly struct Buffer<T>
{
    public static Buffer<T> Empty { get; } = new([]);

    private readonly object? _buffer;
    private readonly int _start;
    private readonly int _length;

    public BufferType Type
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

    public RentedArrayType ArrayType
    {
        get
        {
            if (_buffer is T[])
            {
                if (_start < 0) return _length < 0 ? RentedArrayType.External : RentedArrayType.Global;

                if (_length < 0) return RentedArrayType.Shared;
            }
            return RentedArrayType.None;
        }
    }

    public bool IsNull => _buffer == null;

    public T[]? Array => _buffer as T[];

    public MemoryManager<T>? MemoryManager => _buffer as MemoryManager<T>;

    public IMemoryOwner<T>? MemoryOwner => _buffer as IMemoryOwner<T>;

    public Memory<T> Memory
    {
        get
        {
            var length = Length;
            if (length == 0)
                return default;

            var buffer = _buffer;
            if (buffer is T[] array)
                return new(array, Start, length);

            if (buffer is MemoryManager<T> memoryManager)
                return memoryManager.Memory.Slice(Start, length);

            if (buffer is IMemoryOwner<T> memoryOwner)
                return memoryOwner.Memory.Slice(Start, length);

            throw InvalidState();
        }
    }

    public Span<T> Span
    {
        get
        {
            var length = Length;
            if (length == 0)
                return default;

            var buffer = _buffer;
            if (buffer is T[] array)
                return new(array, Start, length);

            if (buffer is MemoryManager<T> memoryManager)
                return memoryManager.GetSpan().Slice(Start, length);

            if (buffer is IMemoryOwner<T> memoryOwner)
                return memoryOwner.Memory.Span.Slice(Start, length);

            throw InvalidState();
        }
    }

    public int Start => _start < 0 ? ~_start : _start;

    public int Length => _length < 0 ? ~_length : _length;

    public bool IsEmpty => _length == 0 || _length == -1;

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var buffer = _buffer;
            if (buffer is T[] array)
                return array[Start + index];

            if (buffer is MemoryManager<T> memoryManager)
                return memoryManager.GetSpan()[Start + index];

            if (buffer is IMemoryOwner<T> memoryOwner)
                return memoryOwner.Memory.Span[Start + index];

            throw InvalidState();
        }
        set
        {
            if ((uint)index >= (uint)Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var buffer = _buffer;
            if (buffer is T[] array)
            {
                array[Start + index] = value;
            }
            else if (buffer is MemoryManager<T> memoryManager)
            {
                memoryManager.GetSpan()[Start + index] = value;
            }
            else if (buffer is IMemoryOwner<T> memoryOwner)
            {
                memoryOwner.Memory.Span[Start + index] = value;
            }
            else
            {
                throw InvalidState();
            }
        }
    }

    private Buffer(object buffer, int start, int length)
    {
        _buffer = buffer;
        _start = start;
        _length = length;
    }

    public Buffer(T[] array)
    {
        _buffer = array ?? throw new ArgumentNullException(nameof(array));
        _start = 0;
        _length = array.Length;
    }

    public Buffer(T[] array, RentedArrayType arrayType)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        if (arrayType == RentedArrayType.None)
        {
            _start = 0;
            _length = array.Length;
        }
        if (arrayType == RentedArrayType.Shared)
        {
            _start = 0;
            _length = ~array.Length;
        }
        else if (arrayType == RentedArrayType.Global)
        {
            _start = ~0;
            _length = array.Length;
        }
        else if (arrayType == RentedArrayType.External)
        {
            _start = ~0;
            _length = ~array.Length;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(arrayType));
        }

        _buffer = array;
    }

    public Buffer(T[] array, int start, int length, RentedArrayType arrayType)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        if ((uint)start > (uint)array.Length)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(array.Length - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        if (arrayType == RentedArrayType.None)
        {
            _start = start;
            _length = length;
        }
        else if (arrayType == RentedArrayType.Shared)
        {
            _start = start;
            _length = ~length;
        }
        else if (arrayType == RentedArrayType.Global)
        {
            _start = ~start;
            _length = length;
        }
        else if (arrayType == RentedArrayType.External)
        {
            _start = ~start;
            _length = ~length;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(arrayType));
        }

        _buffer = array;
    }

    public Buffer(MemoryManager<T> memoryManager)
    {
        _buffer = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
        _start = 0;
        _length = memoryManager.Memory.Length;
    }

    public Buffer(MemoryManager<T> memoryManager, int start, int length)
    {
        if (memoryManager == null) throw new ArgumentNullException(nameof(memoryManager));
        var memoryManagerLength = memoryManager.Memory.Length;

        if ((uint)start > (uint)memoryManagerLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(memoryManagerLength - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        _buffer = memoryManager;
        _start = start;
        _length = length;
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
            _start = segment.Offset;
            _length = segment.Count;
        }
        else if (MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out var manager, out var start, out var length))
        {
            _buffer = manager;
            _start = start;
            _length = length;
        }
        throw new ArgumentException("Unrecognized memory type", nameof(memory));
    }

    public Buffer(IMemoryOwner<T> memoryOwner)
    {
        _buffer = memoryOwner ?? throw new ArgumentNullException(nameof(memoryOwner));
        _start = 0;
        _length = memoryOwner.Memory.Length;
    }

    public Buffer(IMemoryOwner<T> memoryOwner, int start, int length)
    {
        if (memoryOwner == null) throw new ArgumentNullException(nameof(memoryOwner));
        var memoryOwnerLength = memoryOwner.Memory.Length;

        if ((uint)start > (uint)memoryOwnerLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(memoryOwnerLength - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        _buffer = memoryOwner;
        _start = start;
        _length = length;
    }

    public override int GetHashCode()
        => _buffer is null ? 0 : HashCode.Combine(_start, _length, _buffer.GetHashCode());

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Buffer<T> other && Equals(other);

    public bool Equals(Buffer<T> other)
        => other._buffer == _buffer && other._start == _start && other._length == _length;

    public Buffer<T> Slice(int start)
    {
        var length = Length;
        if ((uint)start > (uint)length)
            throw new ArgumentOutOfRangeException(nameof(start));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Start + start, length - start, ArrayType);

        if (buffer is MemoryManager<T> memoryManager)
            return new((object)memoryManager, Start + start, length - start);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return new((object)memoryOwner, Start + start, length - start);

        throw InvalidState();
    }

    public Buffer<T> Slice(int start, int length)
    {
        var oldLength = Length;
        if ((uint)start > (uint)oldLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(oldLength - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Start + start, length, ArrayType);

        if (buffer is MemoryManager<T> memoryManager)
            return new((object)memoryManager, Start + start, length);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return new((object)memoryOwner, Start + start, length);

        throw InvalidState();
    }

    public T[] ToArray()
    {
        var length = Length;
        if (length == 0) return [];

        var buffer = _buffer;
        if (buffer is T[] array)
        {
            var copy = new T[length];

            System.Array.Copy(array, Start, copy, 0, length);

            return copy;
        }

        if (buffer is MemoryManager<T> memoryManager)
            return memoryManager.GetSpan().Slice(Start, length).ToArray();

        if (buffer is IMemoryOwner<T> memoryOwner)
            return memoryOwner.Memory.Slice(Start, length).ToArray();

        throw InvalidState();
    }

    private static InvalidOperationException InvalidState()
        => new("buffer is invalid");

    public static bool operator ==(Buffer<T> left, Buffer<T> right) => left.Equals(right);

    public static bool operator !=(Buffer<T> left, Buffer<T> right) => !left.Equals(right);

    public static implicit operator Buffer<T>(T[] array) => array != null ? new(array) : default;

    public static implicit operator Memory<T>(Buffer<T> value) => value.Memory;

    public static implicit operator Span<T>(Buffer<T> value) => value.Span;
}