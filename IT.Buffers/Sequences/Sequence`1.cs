using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers;

internal readonly struct Sequence<T>
{
    private readonly object? _startObject;
    private readonly object? _endObject;
    private readonly int _startInteger;
    private readonly int _endInteger;

    public static readonly Sequence<T> Empty = new(Array.Empty<T>());

    public ReadOnlySequence<T> AsReadOnly
    {
        get
        {
            var local = this;
            return Unsafe.As<Sequence<T>, ReadOnlySequence<T>>(ref local);
        }
    }

    public long Length => GetLength();

    public bool IsEmpty => GetLength() == 0;

    public bool IsSingleSegment
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _startObject == _endObject;
    }

    public Memory<T> First => GetFirst();

    public Span<T> FirstSpan => GetFirstSpan();

    public SequencePosition Start
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_startObject, GetIndex(_startInteger));
    }

    public SequencePosition End
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_endObject, GetIndex(_endInteger));
    }

    #region Ctors

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Sequence(object? startSegment, int startIndexAndFlags, object? endSegment, int endIndexAndFlags)
    {
        // Used by SliceImpl to create new Sequence

        // startSegment and endSegment can be null for default Sequence only
        Debug.Assert((startSegment != null && endSegment != null) ||
            (startSegment == null && endSegment == null && startIndexAndFlags == 0 && endIndexAndFlags == 0));

        _startObject = startSegment;
        _endObject = endSegment;
        _startInteger = startIndexAndFlags;
        _endInteger = endIndexAndFlags;
    }

    public Sequence(SequenceSegment<T> startSegment, int startIndex, SequenceSegment<T> endSegment, int endIndex)
    {
        if (startSegment == null) throw new ArgumentNullException(nameof(startSegment));
        if (endSegment == null) throw new ArgumentNullException(nameof(endSegment));

        if ((startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex) ||
            (uint)startSegment.Memory.Length < (uint)startIndex ||
            (uint)endSegment.Memory.Length < (uint)endIndex ||
            (startSegment == endSegment && endIndex < startIndex))
            throw new ArgumentOutOfRangeException();

        _startObject = startSegment;
        _endObject = endSegment;
        _startInteger = startIndex;
        _endInteger = endIndex;
    }

    public Sequence(T[] array)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        _startObject = array;
        _endObject = array;
        _startInteger = 0;
        _endInteger = SequenceFlags.ArrayToSequenceEnd(array.Length);
    }

    public Sequence(T[] array, int start, int length)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        if ((uint)start > (uint)array.Length ||
            (uint)length > (uint)(array.Length - start))
            throw new ArgumentOutOfRangeException();

        _startObject = array;
        _endObject = array;
        _startInteger = start;
        _endInteger = SequenceFlags.ArrayToSequenceEnd(start + length);
    }

    public Sequence(Memory<T> memory)
    {
        if (MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out var manager, out int start, out int length))
        {
            _startObject = manager;
            _endObject = manager;
            _startInteger = SequenceFlags.MemoryManagerToSequenceStart(start);
            _endInteger = start + length;
        }
        else if (MemoryMarshal.TryGetArray<T>(memory, out var segment))
        {
            T[]? array = segment.Array;
            int offset = segment.Offset;
            _startObject = array;
            _endObject = array;
            _startInteger = offset;
            _endInteger = SequenceFlags.ArrayToSequenceEnd(offset + segment.Count);
        }
        else
        {
            ThrowInvalidMemoryType(memory);
        }
    }

    #endregion Ctors

    #region Slicing

    public Sequence<T> Slice(long start, long length)
    {
        var local = AsReadOnly.Slice(start, length);
        return Unsafe.As<ReadOnlySequence<T>, Sequence<T>>(ref local);
    }

    public Sequence<T> Slice(long start, SequencePosition end)
    {
        var local = AsReadOnly.Slice(start, end);
        return Unsafe.As<ReadOnlySequence<T>, Sequence<T>>(ref local);
    }

    public Sequence<T> Slice(SequencePosition start, long length)
    {
        var local = AsReadOnly.Slice(start, length);
        return Unsafe.As<ReadOnlySequence<T>, Sequence<T>>(ref local);
    }

    public Sequence<T> Slice(int start, int length) => Slice((long)start, length);

    public Sequence<T> Slice(int start, SequencePosition end) => Slice((long)start, end);

    public Sequence<T> Slice(SequencePosition start, int length) => Slice(start, (long)length);

    public Sequence<T> Slice(SequencePosition start, SequencePosition end)
    {
        var local = AsReadOnly.Slice(start, end);
        return Unsafe.As<ReadOnlySequence<T>, Sequence<T>>(ref local);
    }

    public Sequence<T> Slice(SequencePosition start)
    {
        var local = AsReadOnly.Slice(start);
        return Unsafe.As<ReadOnlySequence<T>, Sequence<T>>(ref local);
    }

    public Sequence<T> Slice(long start)
    {
        var local = AsReadOnly.Slice(start);
        return Unsafe.As<ReadOnlySequence<T>, Sequence<T>>(ref local);
    }

    #endregion Slicing

    /// <inheritdoc />
    public override string ToString()
    {
        if (typeof(T) == typeof(char))
        {
            Sequence<T> local = this;
            var charSequence = Unsafe.As<Sequence<T>, ReadOnlySequence<char>>(ref local);
            
            var length = Length;
            if (length <= BufferSize.Max_String)
            {
                return string.Create((int)length, charSequence, (span, sequence) => sequence.CopyTo(span));
            }
        }

        return $"IT.Buffers.Sequence<{typeof(T).Name}>[{Length}]";
    }

    public Enumerator GetEnumerator() => new(this);

    public SequencePosition GetPosition(long offset) => AsReadOnly.GetPosition(offset);

#if NET
    public long GetOffset(SequencePosition position) => AsReadOnly.GetOffset(position);
#endif

    public SequencePosition GetPosition(long offset, SequencePosition origin) =>
        AsReadOnly.GetPosition(offset, origin);

    public bool TryGet(ref SequencePosition position, out Memory<T> memory, bool advance = true)
    {
        bool result = TryGetBuffer(position, out memory, out SequencePosition next);
        if (advance)
        {
            position = next;
        }
        return result;
    }

    #region Private

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetBuffer(in SequencePosition position, out Memory<T> memory, out SequencePosition next)
    {
        object? positionObject = position.GetObject();
        next = default;

        if (positionObject == null)
        {
            memory = default;
            return false;
        }

        SequenceType type = GetSequenceType();
        object? endObject = _endObject;
        int startIndex = position.GetInteger();
        int endIndex = GetIndex(_endInteger);

        if (type == SequenceType.MultiSegment)
        {
            Debug.Assert(positionObject is SequenceSegment<T>);

            SequenceSegment<T> startSegment = (SequenceSegment<T>)positionObject;

            if (startSegment != endObject)
            {
                SequenceSegment<T>? nextSegment = startSegment.Next;

                if (nextSegment == null)
                    ThrowInvalidOperationException_EndPositionNotReached();

                next = new SequencePosition(nextSegment, 0);
                memory = startSegment.Memory.Slice(startIndex);
            }
            else
            {
                memory = startSegment.Memory.Slice(startIndex, endIndex - startIndex);
            }
        }
        else
        {
            if (positionObject != endObject)
                ThrowInvalidOperationException_EndPositionNotReached();

            if (type == SequenceType.Array)
            {
                Debug.Assert(positionObject is T[]);

                memory = new Memory<T>((T[])positionObject, startIndex, endIndex - startIndex);
            }
            else // type == SequenceType.MemoryManager
            {
                Debug.Assert(type == SequenceType.MemoryManager);
                Debug.Assert(positionObject is MemoryManager<T>);

                memory = ((MemoryManager<T>)positionObject).Memory.Slice(startIndex, endIndex - startIndex);
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Memory<T> GetFirst()
    {
        object? startObject = _startObject;

        if (startObject == null)
            return default;

        int startIndex = _startInteger;
        int endIndex = _endInteger;

        bool isMultiSegment = startObject != _endObject;

        // The highest bit of startIndex and endIndex are used to infer the sequence type
        // The code below is structured this way for performance reasons and is equivalent to the following:
        // SequenceType type = GetSequenceType();
        // if (type == SequenceType.MultiSegment) { ... }
        // else if (type == SequenceType.Array) { ... }
        // else if (type == SequenceType.String){ ... }
        // else if (type == SequenceType.MemoryManager) { ... }

        // Highest bit of startIndex: A = startIndex >> 31
        // Highest bit of endIndex: B = endIndex >> 31

        // A == 0 && B == 0 means SequenceType.MultiSegment
        // Equivalent to startIndex >= 0 && endIndex >= 0
        if ((startIndex | endIndex) >= 0)
        {
            Memory<T> memory = ((SequenceSegment<T>)startObject).Memory;
            if (isMultiSegment)
            {
                return memory.Slice(startIndex);
            }
            return memory.Slice(startIndex, endIndex - startIndex);
        }
        else
        {
            return GetFirstSlow(startObject, isMultiSegment);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Memory<T> GetFirstSlow(object startObject, bool isMultiSegment)
    {
        if (isMultiSegment)
            ThrowInvalidOperationException_EndPositionNotReached();

        int startIndex = _startInteger;
        int endIndex = _endInteger;

        Debug.Assert(startIndex < 0 || endIndex < 0);

        // A == 0 && B == 1 means SequenceType.Array
        if (startIndex >= 0)
        {
            Debug.Assert(endIndex < 0);
            return new Memory<T>((T[])startObject, startIndex, (endIndex & SequenceFlags.IndexBitMask) - startIndex);
        }
        else
        {
            startIndex &= SequenceFlags.IndexBitMask;
            return ((MemoryManager<T>)startObject).Memory.Slice(startIndex, endIndex - startIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<T> GetFirstSpan()
    {
        object? startObject = _startObject;

        if (startObject == null)
            return default;

        int startIndex = _startInteger;
        int endIndex = _endInteger;

        bool isMultiSegment = startObject != _endObject;

        // The highest bit of startIndex and endIndex are used to infer the sequence type
        // The code below is structured this way for performance reasons and is equivalent to the following:
        // SequenceType type = GetSequenceType();
        // if (type == SequenceType.MultiSegment) { ... }
        // else if (type == SequenceType.Array) { ... }
        // else if (type == SequenceType.String){ ... }
        // else if (type == SequenceType.MemoryManager) { ... }

        // Highest bit of startIndex: A = startIndex >> 31
        // Highest bit of endIndex: B = endIndex >> 31

        // A == 0 && B == 0 means SequenceType.MultiSegment
        // Equivalent to startIndex >= 0 && endIndex >= 0
        if ((startIndex | endIndex) >= 0)
        {
            Span<T> span = ((SequenceSegment<T>)startObject).Memory.Span;
            if (isMultiSegment)
            {
                return span.Slice(startIndex);
            }
            return span.Slice(startIndex, endIndex - startIndex);
        }
        else
        {
            return GetFirstSpanSlow(startObject, isMultiSegment);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Span<T> GetFirstSpanSlow(object startObject, bool isMultiSegment)
    {
        if (isMultiSegment)
            ThrowInvalidOperationException_EndPositionNotReached();

        int startIndex = _startInteger;
        int endIndex = _endInteger;

        Debug.Assert(startIndex < 0 || endIndex < 0);

        // A == 0 && B == 1 means SequenceType.Array
        if (startIndex >= 0)
        {
            Debug.Assert(endIndex < 0);
            Span<T> span = (T[])startObject;
            return span.Slice(startIndex, (endIndex & SequenceFlags.IndexBitMask) - startIndex);
        }
        else
        {
            startIndex &= SequenceFlags.IndexBitMask;
            return ((MemoryManager<T>)startObject).GetSpan().Slice(startIndex, endIndex - startIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SequenceType GetSequenceType()
    {
        // We take high order bits of two indexes and move them
        // to a first and second position to convert to SequenceType

        // if (start < 0  and end < 0)
        // start >> 31 = -1, end >> 31 = -1
        // 2 * (-1) + (-1) = -3, result = (SequenceType)3

        // if (start < 0  and end >= 0)
        // start >> 31 = -1, end >> 31 = 0
        // 2 * (-1) + 0 = -2, result = (SequenceType)2

        // if (start >= 0  and end >= 0)
        // start >> 31 = 0, end >> 31 = 0
        // 2 * 0 + 0 = 0, result = (SequenceType)0

        // if (start >= 0  and end < 0)
        // start >> 31 = 0, end >> 31 = -1
        // 2 * 0 + (-1) = -1, result = (SequenceType)1

        return (SequenceType)(-(2 * (_startInteger >> 31) + (_endInteger >> 31)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndex(int value) => value & SequenceFlags.IndexBitMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetLength()
    {
        object? startObject = _startObject;
        object? endObject = _endObject;
        int startIndex = GetIndex(_startInteger);
        int endIndex = GetIndex(_endInteger);

        if (startObject != endObject)
        {
            var startSegment = (SequenceSegment<T>)startObject!;
            var endSegment = (SequenceSegment<T>)endObject!;
            // (End offset) - (start offset)
            return (endSegment.RunningIndex + endIndex) - (startSegment.RunningIndex + startIndex);
        }

        // Single segment length
        return endIndex - startIndex;
    }

    private static void ThrowInvalidOperationException_EndPositionNotReached()=>
        throw new InvalidOperationException("EndPositionNotReached");

    private static void ThrowInvalidMemoryType(Memory<T> memory)
    {
        if (typeof(T) == typeof(char) && MemoryMarshal.TryGetString(Unsafe.As<Memory<T>, Memory<char>>(ref memory), out _, out _, out _))
        {
            throw new ArgumentException("Invalid memory type. String not supported.", nameof(memory));
        }
        throw new ArgumentException("Unrecognized memory type.", nameof(memory));
    }

    #endregion Private

    public struct Enumerator
    {
        private readonly Sequence<T> _sequence;
        private SequencePosition _next;
        private Memory<T> _current;

        public Enumerator(in Sequence<T> sequence)
        {
            _current = default;
            _next = sequence.Start;
            _sequence = sequence;
        }

        public readonly Memory<T> Current => _current;

        public bool MoveNext()
        {
            if (_next.GetObject() == null)
            {
                return false;
            }

            return _sequence.TryGet(ref _next, out _current);
        }
    }
}