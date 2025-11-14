using System;

namespace IT.Buffers;

public enum RentedArrayType : byte
{
    /// <summary>
    /// Not rented
    /// </summary>
    None = 0,

    /// <summary>
    /// ArrayPool<T>.Shared.Rent
    /// </summary>
    Shared = 1,

    Global = 2,

    Custom = 3
}

public readonly struct RentedArray<T>
{
    private readonly T[]? _array;
    private readonly int _offset;
    private readonly int _count;

    public RentedArrayType Type
    {
        get
        {
            if (_offset < 0) return _count < 0 ? RentedArrayType.Custom : RentedArrayType.Global;

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

            return _array![Offset + index];
        }
        set
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _array![Offset + index] = value;
        }
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
        else if (type == RentedArrayType.Custom)
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
}