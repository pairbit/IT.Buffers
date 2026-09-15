using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers;

public readonly struct Buffer<T> : IEquatable<Buffer<T>>
{
#pragma warning disable CA1825 // Avoid zero-length array allocations
    public static Buffer<T> Empty { get; } = new((object)new T[0], default, default);
#pragma warning restore CA1825 // Avoid zero-length array allocations

    private readonly object? _buffer;
    private readonly int _start;
    private readonly int _length;

    #region Props

    private RentedArrayType RentedType
    {
        get
        {
            if (_start < 0) return _length < 0 ? RentedArrayType.External : RentedArrayType.Global;

            return _length < 0 ? RentedArrayType.Shared : RentedArrayType.None;
        }
    }

    public BufferType Type
    {
        get
        {
            var buffer = _buffer;
            if (buffer is T[]) return BufferType.Array;
            if (buffer is MemoryManager<T>) return BufferType.MemoryManager;
            if (buffer is IMemoryOwner<T>) return BufferType.MemoryOwner;

            //TODO: что если SequenceSegment будет наследовать IMemoryOwner или ISequenceOwner?
            if (buffer is SequenceSegment<T>) return BufferType.Sequence;
            if (buffer is ISequenceOwner<T>) return BufferType.SequenceOwner;

            return buffer is null ? BufferType.Null : BufferType.Unknown;
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

    public MemoryManager<T>? MemoryManager => IsRented ? _buffer as MemoryManager<T> : null;

    public IMemoryOwner<T>? MemoryOwner => IsRented ? _buffer as IMemoryOwner<T> : null;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public MemoryManager<T>? UnsafeMemoryManager => _buffer as MemoryManager<T>;
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IMemoryOwner<T>? UnsafeMemoryOwner => _buffer as IMemoryOwner<T>;

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

            if (buffer is IMemoryOwner<T> memoryOwner)
                return memoryOwner.Memory.Slice(Start, length);

            if (buffer is SequenceSegment<T> || buffer is ISequenceOwner<T>)
                throw new NotSupportedException("The sequence does not support memory.");

            throw BufferUnknown();
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

            if (buffer is SequenceSegment<T> || buffer is ISequenceOwner<T>)
                throw new NotSupportedException("The sequence does not support span.");

            throw BufferUnknown();
        }
    }

    internal ISequenceOwner<T>? SequenceOwner => _buffer as ISequenceOwner<T>;

    //internal Sequence<T> Sequence => GetSequence();

    public int Start => _start < 0 ? ~_start : _start;

    public int Length => _length < 0 ? ~_length : _length;

    public bool IsEmpty => _length == 0 || _length == -1;

    public bool IsRented => _length < 0 || _start < 0;

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

            throw BufferUnknown();
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
                throw BufferUnknown();
            }
        }
    }

    #endregion Props

    #region Ctors

    private Buffer(object? buffer, int start, int length)
    {
        _buffer = buffer;
        _start = start;
        _length = length;
    }

    public Buffer(T[] array)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        _buffer = array;
        _start = 0;
        _length = array.Length;
    }

    public Buffer(T[] array, RentedArrayType arrayType)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        var length = array.Length;
        if (arrayType == RentedArrayType.None)
        {
            _start = 0;
            _length = length;
        }
        if (arrayType == RentedArrayType.Shared)
        {
            if (length == 0)
                throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

            _start = 0;
            _length = ~length;
        }
        else if (arrayType == RentedArrayType.Global)
        {
            if (length == 0)
                throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

            _start = ~0;
            _length = length;
        }
        else if (arrayType == RentedArrayType.External)
        {
            if (length == 0)
                throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

            _start = ~0;
            _length = ~length;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(arrayType));
        }

        _buffer = array;
    }

    public Buffer(T[] array, int start, int length)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        var arrayLength = array.Length;
        if ((uint)start > (uint)arrayLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(arrayLength - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        _buffer = array;
        _start = start;
        _length = length;
    }

    public Buffer(T[] array, int start, int length, RentedArrayType arrayType)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        var arrayLength = array.Length;
        if ((uint)start > (uint)arrayLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(arrayLength - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        if (arrayType == RentedArrayType.None)
        {
            _start = start;
            _length = length;
        }
        else if (arrayType == RentedArrayType.Shared)
        {
            if (arrayLength == 0)
                throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

            _start = start;
            _length = ~length;
        }
        else if (arrayType == RentedArrayType.Global)
        {
            if (arrayLength == 0)
                throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

            _start = ~start;
            _length = length;
        }
        else if (arrayType == RentedArrayType.External)
        {
            if (arrayLength == 0)
                throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

            _start = ~start;
            _length = ~length;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(arrayType));
        }

        _buffer = array;
    }

    public Buffer(ArraySegment<T> segment)
    {
        _buffer = segment.Array;
        _start = segment.Offset;
        _length = segment.Count;
    }

    public Buffer(ArraySegment<T> segment, RentedArrayType arrayType)
    {
        var array = segment.Array;
        if (array == null)
        {
            if (arrayType != RentedArrayType.None)
            {
                if (arrayType == RentedArrayType.Shared || arrayType == RentedArrayType.Global || arrayType == RentedArrayType.External)
                    throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

                throw new ArgumentOutOfRangeException(nameof(arrayType));
            }
            this = default;
        }
        else
        {
            if (arrayType == RentedArrayType.None)
            {
                _start = segment.Offset;
                _length = segment.Count;
            }
            else if (arrayType == RentedArrayType.Shared)
            {
                if (array.Length == 0)
                    throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

                _start = segment.Offset;
                _length = ~segment.Count;
            }
            else if (arrayType == RentedArrayType.Global)
            {
                if (array.Length == 0)
                    throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

                _start = ~segment.Offset;
                _length = segment.Count;
            }
            else if (arrayType == RentedArrayType.External)
            {
                if (array.Length == 0)
                    throw new ArgumentException("Empty array cannot be rented.", nameof(arrayType));

                _start = ~segment.Offset;
                _length = ~segment.Count;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(arrayType));
            }

            _buffer = array;
        }
    }

    public Buffer(Memory<T> memory)
    {
        // получаем сначала менеджера, чтобы случайно не потерять на него ссылку
        // с другой стороны зачем нам он нужен, если при получении Manager из Memory, он не арендован
        if (MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out var manager, out var start, out var length))
        {
            _buffer = manager;
            _start = start;
            _length = length;
        }
        else if (MemoryMarshal.TryGetArray((ReadOnlyMemory<T>)memory, out var segment))
        {
            _buffer = segment.Array;
            _start = segment.Offset;
            _length = segment.Count;
        }
        else
        {
            Throw();
            static void Throw() => throw new ArgumentException("Unrecognized memory type.", nameof(memory));
        }
    }

    public Buffer(IMemoryOwner<T> memoryOwner, bool isRented = true)
    {
        _buffer = memoryOwner ?? throw new ArgumentNullException(nameof(memoryOwner));
        _start = 0;
        _length = isRented ? ~memoryOwner.Memory.Length : memoryOwner.Memory.Length;
    }

    public Buffer(IMemoryOwner<T> memoryOwner, int start, int length, bool isRented = true)
    {
        if (memoryOwner == null) throw new ArgumentNullException(nameof(memoryOwner));
        var memoryLength = memoryOwner.Memory.Length;

        if ((uint)start > (uint)memoryLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(memoryLength - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        _buffer = memoryOwner;
        _start = start;
        _length = isRented ? ~length : length;
    }

    /*
     public Buffer(Sequence<T> sequence, int start, int length)
     public Buffer(ISequenceOwner<T> sequenceOwner, int start, int length)
     */

    #endregion Ctors

    public override int GetHashCode()
        => _buffer is null ? 0 : HashCode.Combine(_buffer.GetHashCode(), _start, _length);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Buffer<T> other && Equals(other);

    public bool Equals(Buffer<T> other)
        => other._buffer == _buffer && other._start == _start && other._length == _length;

    #region Slicing

    public Buffer<T> Slice(int start)
    {
        var length = Length;
        if ((uint)start > (uint)length)
            throw new ArgumentOutOfRangeException(nameof(start));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Start + start, length - start, ArrayType);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return new(memoryOwner, Start + start, length - start, IsRented);

        if (buffer == null)
            return length == 0 ? default : ThrowBufferStateInvalid();

        throw BufferUnknown();
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

        if (buffer is IMemoryOwner<T> memoryOwner)
            return new(memoryOwner, Start + start, length, IsRented);

        if (buffer == null)
            return length == 0 ? default : ThrowBufferStateInvalid();

        throw BufferUnknown();
    }

    public Buffer<T> AsUnrented() => new(_buffer, Start, Length);

    public Buffer<T> AsUnrented(int start)
    {
        var length = Length;
        if ((uint)start > (uint)length)
            throw new ArgumentOutOfRangeException(nameof(start));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Start + start, length - start);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return new(memoryOwner, Start + start, length - start, isRented: false);

        if (buffer == null)
            return length == 0 ? default : ThrowBufferStateInvalid();

        throw BufferUnknown();
    }

    public Buffer<T> AsUnrented(int start, int length)
    {
        var oldLength = Length;
        if ((uint)start > (uint)oldLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(oldLength - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Start + start, length);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return new(memoryOwner, Start + start, length, isRented: false);

        if (buffer == null)
            return length == 0 ? default : ThrowBufferStateInvalid();

        throw BufferUnknown();
    }

    public Memory<T> AsMemory(int start)
    {
        var length = Length;
        if ((uint)start > (uint)length)
            throw new ArgumentOutOfRangeException(nameof(start));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Start + start, length - start);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return memoryOwner.Memory.Slice(Start + start, length - start);

        if (buffer is SequenceSegment<T> || buffer is ISequenceOwner<T>)
            throw new NotSupportedException("The sequence does not support memory.");

        if (buffer == null)
            return length == 0 ? default : ThrowBufferStateInvalid();

        throw BufferUnknown();
    }

    public Memory<T> AsMemory(int start, int length)
    {
        var oldLength = Length;
        if ((uint)start > (uint)oldLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(oldLength - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Start + start, length);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return memoryOwner.Memory.Slice(Start + start, length);

        if (buffer is SequenceSegment<T> || buffer is ISequenceOwner<T>)
            throw new NotSupportedException("The sequence does not support memory.");

        if (buffer == null)
            return length == 0 ? default : ThrowBufferStateInvalid();

        throw BufferUnknown();
    }

    public Span<T> AsSpan(int start)
    {
        var length = Length;
        if ((uint)start > (uint)length)
            throw new ArgumentOutOfRangeException(nameof(start));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Start + start, length - start);

        if (buffer is MemoryManager<T> memoryManager)
            return memoryManager.GetSpan().Slice(Start + start, length - start);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return memoryOwner.Memory.Span.Slice(Start + start, length - start);

        if (buffer is SequenceSegment<T> || buffer is ISequenceOwner<T>)
            throw new NotSupportedException("The sequence does not support span.");

        if (buffer == null)
            return length == 0 ? default : ThrowBufferStateInvalid();

        throw BufferUnknown();
    }

    public Span<T> AsSpan(int start, int length)
    {
        var oldLength = Length;
        if ((uint)start > (uint)oldLength)
            throw new ArgumentOutOfRangeException(nameof(start));

        if ((uint)length > (uint)(oldLength - start))
            throw new ArgumentOutOfRangeException(nameof(length));

        var buffer = _buffer;
        if (buffer is T[] array)
            return new(array, Start + start, length);

        if (buffer is MemoryManager<T> memoryManager)
            return memoryManager.GetSpan().Slice(Start + start, length);

        if (buffer is IMemoryOwner<T> memoryOwner)
            return memoryOwner.Memory.Span.Slice(Start + start, length);

        if (buffer is SequenceSegment<T> || buffer is ISequenceOwner<T>)
            throw new NotSupportedException("The sequence does not support span.");

        if (buffer == null)
            return length == 0 ? default : ThrowBufferStateInvalid();

        throw BufferUnknown();
    }

    #endregion Slicing

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

        throw BufferUnknown();
    }

    public Buffer<T> AsEmpty() => new(_buffer, _start, _length < 0 ? -1 : 0);

    public Buffer<T> CopyIfRented() => IsRented ? ToArray() : this;

    /// <exception cref="InvalidOperationException">Empty array cannot be rented.</exception>
    /// <exception cref="NotImplementedException">GlobalArrayPool not implemented.</exception>
    public bool TryReturn(out T[]? externalArray)
    {
        var rentedType = RentedType;
        if (rentedType == RentedArrayType.None)
        {
            externalArray = default;
            return false;
        }

        var buffer = _buffer;
        if (buffer is T[] array)
        {
            if (array.Length == 0)
                throw new InvalidOperationException("Empty array cannot be rented.");

            if (rentedType == RentedArrayType.Shared)
            {
                ArrayPool<T>.Shared.Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                externalArray = default;
                return true;
            }

            if (rentedType == RentedArrayType.Global)
            {
                throw new NotImplementedException();
                //GlobalArrayPool.Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            }

            Debug.Assert(rentedType == RentedArrayType.External);

            externalArray = array;
            return false;
        }

        if (buffer is IMemoryOwner<T> memoryOwner)
        {
            memoryOwner.Dispose();
            externalArray = default;
            return true;
        }

        if (buffer is ISequenceOwner<T> sequenceOwner)
        {
            sequenceOwner.Dispose();
            externalArray = default;
            return true;
        }

        if (buffer is SequenceSegment<T> sequenceSegment)
        {
            //TODO: SequenceSegmentPool<T>.Return(sequenceSegment)???
            var count = BufferPool.TryResetSegments(sequenceSegment);
            Debug.Assert(count > 0);

            externalArray = default;
            return true;
        }

        if (buffer == null)
        {
            throw BufferStateInvalid();
        }

        throw BufferUnknown();
    }

    /// <exception cref="InvalidOperationException">It is impossible to return an external array.</exception>
    /// <exception cref="NotImplementedException">GlobalArrayPool not implemented.</exception>
    public void Return()
    {
        var rentedType = RentedType;
        if (rentedType != RentedArrayType.None)
        {
            var buffer = _buffer;
            if (buffer is T[] array)
            {
                Debug.Assert(array.Length > 0, "Empty array cannot be rented.");

                if (rentedType == RentedArrayType.Shared)
                {
                    ArrayPool<T>.Shared.Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                }
                else if (rentedType == RentedArrayType.Global)
                {
                    throw new NotImplementedException("GlobalArrayPool not implemented.");
                    //GlobalArrayPool.Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                }
                else
                {
                    Debug.Assert(rentedType == RentedArrayType.External);

                    throw new InvalidOperationException("It is impossible to return an external array.");
                }
            }
            else if (buffer is IMemoryOwner<T> memoryOwner)
            {
                memoryOwner.Dispose();
            }
            else if (buffer is ISequenceOwner<T> sequenceOwner)
            {
                sequenceOwner.Dispose();
            }
            else if (buffer is SequenceSegment<T> sequenceSegment)
            {
                //TODO: SequenceSegmentPool<T>.Return(sequenceSegment)???
                var count = BufferPool.TryResetSegments(sequenceSegment);
                Debug.Assert(count > 0);
            }
            else if (buffer == null)
            {
                throw BufferStateInvalid();
            }
            else
            {
                throw BufferUnknown();
            }
        }
    }

    private static InvalidOperationException BufferUnknown() => new("buffer is unknown.");

    private static InvalidOperationException BufferStateInvalid() => throw new("buffer state is invalid.");

    private static Buffer<T> ThrowBufferStateInvalid() => throw BufferStateInvalid();

    #region Operators

    public static bool operator ==(Buffer<T> left, Buffer<T> right) => left.Equals(right);

    public static bool operator !=(Buffer<T> left, Buffer<T> right) => !left.Equals(right);

    public static implicit operator Buffer<T>(ArraySegment<T> segment) => new(segment);

    public static implicit operator Buffer<T>(T[]? array) => array != null ? new(array) : default;

    public static implicit operator Buffer<T>(Memory<T> memory) => new(memory);

    public static implicit operator Memory<T>(Buffer<T> buffer) => buffer.Memory;

    public static implicit operator ReadOnlyMemory<T>(Buffer<T> buffer) => buffer.Memory;

    public static implicit operator Span<T>(Buffer<T> buffer) => buffer.Span;

    public static implicit operator ReadOnlySpan<T>(Buffer<T> buffer) => buffer.Span;

    #endregion Operators
}