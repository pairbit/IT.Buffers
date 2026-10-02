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

    public ReadOnlySequence<T> AsReadOnly => Unsafe.As<Sequence<T>, ReadOnlySequence<T>>(ref Unsafe.AsRef(in this));

    public long Length => AsReadOnly.Length;

    public bool IsEmpty => AsReadOnly.IsEmpty;

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

    public Sequence(ReadOnlySequenceSegment<T> startSegment, int startIndex, ReadOnlySequenceSegment<T> endSegment, int endIndex)
    {
        if (startSegment == null ||
            endSegment == null ||
            (startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex) ||
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
        _endInteger = Flags.ArrayToSequenceEnd(array.Length);
    }

    public Sequence(T[] array, int start, int length)
    {
        if (array == null ||
            (uint)start > (uint)array.Length ||
            (uint)length > (uint)(array.Length - start))
            throw new ArgumentOutOfRangeException();

        _startObject = array;
        _endObject = array;
        _startInteger = start;
        _endInteger = Flags.ArrayToSequenceEnd(start + length);
    }

    public Sequence(Memory<T> memory)
    {
        if (MemoryMarshal.TryGetMemoryManager((ReadOnlyMemory<T>)memory, out MemoryManager<T>? manager, out int start, out int length))
        {
            _startObject = manager;
            _endObject = manager;
            _startInteger = Flags.MemoryManagerToSequenceStart(start);
            _endInteger = start + length;
        }
        else if (MemoryMarshal.TryGetArray(memory, out ArraySegment<T> segment))
        {
            T[]? array = segment.Array;
            int offset = segment.Offset;
            _startObject = array;
            _endObject = array;
            _startInteger = offset;
            _endInteger = Flags.ArrayToSequenceEnd(offset + segment.Count);
        }
        else
        {
            Throw();
            static void Throw() => throw new ArgumentException("Unrecognized memory type.", nameof(memory));
        }
    }

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

    /// <inheritdoc />
    public override string ToString()
    {
        if (typeof(T) == typeof(char))
        {
            Sequence<T> local = this;
            var charSequence = Unsafe.As<Sequence<T>, ReadOnlySequence<char>>(ref local);

            if (Length < int.MaxValue)
            {
                return string.Create((int)Length, charSequence, (span, sequence) => sequence.CopyTo(span));
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
        var status = AsReadOnly.TryGet(ref position, out var readOnlyMemory, advance);

        memory = MemoryMarshal.AsMemory(readOnlyMemory);
        return status;
    }

    #region Private

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
            throw new InvalidOperationException("EndPositionNotReached");

        int startIndex = _startInteger;
        int endIndex = _endInteger;

        Debug.Assert(startIndex < 0 || endIndex < 0);

        // A == 0 && B == 1 means SequenceType.Array
        if (startIndex >= 0)
        {
            Debug.Assert(endIndex < 0);
            return new Memory<T>((T[])startObject, startIndex, (endIndex & Flags.IndexBitMask) - startIndex);
        }
        else
        {
            startIndex &= Flags.IndexBitMask;
            return ((MemoryManager<T>)startObject).Memory.Slice(startIndex, endIndex - startIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndex(int value) => value & Flags.IndexBitMask;

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
            throw new InvalidOperationException("EndPositionNotReached");

        int startIndex = _startInteger;
        int endIndex = _endInteger;

        Debug.Assert(startIndex < 0 || endIndex < 0);

        // A == 0 && B == 1 means SequenceType.Array
        if (startIndex >= 0)
        {
            Debug.Assert(endIndex < 0);
            Span<T> span = (T[])startObject;
            return span.Slice(startIndex, (endIndex & Flags.IndexBitMask) - startIndex);
        }
        else
        {
            startIndex &= Flags.IndexBitMask;
            return ((MemoryManager<T>)startObject).GetSpan().Slice(startIndex, endIndex - startIndex);
        }
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

    static class Flags
    {
        public const int FlagBitMask = 1 << 31;
        public const int IndexBitMask = ~FlagBitMask;

        public const int ArrayEndMask = FlagBitMask;

        public const int MemoryManagerStartMask = FlagBitMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ArrayToSequenceEnd(int endIndex) => endIndex | ArrayEndMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MemoryManagerToSequenceStart(int startIndex) => startIndex | MemoryManagerStartMask;
    }
}