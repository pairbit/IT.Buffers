using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

public readonly struct RentedArray<T>
{
    public static RentedArray<T> Empty { get; } = new([]);

    private readonly T[]? _array;
    private readonly int _offset;
    private readonly int _count;

    public RentedArrayType Type
    {
        get
        {
            if (_offset < 0) return _count < 0 ? RentedArrayType.External : RentedArrayType.Global;

            if (_count < 0) return RentedArrayType.Shared;

            return RentedArrayType.None;
        }
    }

    public T[]? Array => _array;

    public int Offset => _offset < 0 ? ~_offset : _offset;

    public int Count => _count < 0 ? ~_count : _count;

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            Debug.Assert(_array != null);

            return _array[Offset + index];
        }
        set
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            Debug.Assert(_array != null);

            _array[Offset + index] = value;
        }
    }

    public RentedArray(T[] array)
    {
        _array = array ?? throw new ArgumentNullException(nameof(array));
        _offset = 0;
        _count = array.Length;
    }

    public RentedArray(T[] array, RentedArrayType type)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        if (type == RentedArrayType.Shared)
        {
            _array = array;
            _offset = 0;
            _count = ~array.Length;
        }
        else if (type == RentedArrayType.Global)
        {
            _array = array;
            _offset = ~0;
            _count = array.Length;
        }
        else if (type == RentedArrayType.External)
        {
            _array = array;
            _offset = ~0;
            _count = ~array.Length;
        }
        else if (type == RentedArrayType.None)
        {
            _array = array;
            _offset = 0;
            _count = array.Length;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public RentedArray(T[] array, int offset, int count, RentedArrayType type)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        if ((uint)offset > (uint)array.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((uint)count > (uint)(array.Length - offset))
            throw new ArgumentOutOfRangeException(nameof(count));

        _array = array;
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

    public override int GetHashCode()
        => _array is null ? 0 : HashCode.Combine(_offset, _count, _array.GetHashCode());

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is RentedArray<T> other && Equals(other);

    public bool Equals(RentedArray<T> other)
        => other._array == _array && other._offset == _offset && other._count == _count;

    public RentedArray<T> Slice(int index)
    {
        var count = Count;
        if ((uint)index > (uint)count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return new(_array ?? throw InvalidState(), Offset + index, count - index, Type);
    }

    public RentedArray<T> Slice(int index, int count)
    {
        var oldCount = Count;
        if ((uint)index > (uint)oldCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        if ((uint)count > (uint)(oldCount - index))
            throw new ArgumentOutOfRangeException(nameof(count));

        return new(_array ?? throw InvalidState(), Offset + index, count, Type);
    }

    public T[] ToArray()
    {
        if (_count == 0) return Empty._array!;

        var array = _array ?? throw InvalidState();
        var copy = new T[_count];

        System.Array.Copy(array, _offset, copy, 0, _count);

        return copy;
    }

    private static InvalidOperationException InvalidState()
        => new("_array == null");

    public static bool operator ==(RentedArray<T> left, RentedArray<T> right) => left.Equals(right);

    public static bool operator !=(RentedArray<T> left, RentedArray<T> right) => !left.Equals(right);

    public static implicit operator RentedArray<T>(T[] array) => array != null ? new(array) : default;

    public static implicit operator ArraySegment<T>(RentedArray<T> value)
        => value._array != null ? new(value._array, value.Offset, value.Count) : default;
}