/*
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers;


public readonly struct Sequence<T>
{
    private readonly object? _startObject;
    private readonly object? _endObject;
    private readonly int _startInteger;
    private readonly int _endInteger;

    public static readonly Sequence<T> Empty = new Sequence<T>(Array.Empty<T>());

    public long Length => GetLength();

    public bool IsEmpty => Length == 0;

    public bool IsSingleSegment
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _startObject == _endObject;
    }

    public Memory<T> First => GetFirstBuffer();

    public Span<T> FirstSpan => GetFirstSpan();

    public SequencePosition Start
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new SequencePosition(_startObject, GetIndex(_startInteger));
    }

    public SequencePosition End
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new SequencePosition(_endObject, GetIndex(_endInteger));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Sequence(object? startSegment, int startIndexAndFlags, object? endSegment, int endIndexAndFlags)
    {
        // Used by SliceImpl to create new ReadOnlySequence

        // startSegment and endSegment can be null for default ReadOnlySequence only
        Debug.Assert((startSegment != null && endSegment != null) ||
            (startSegment == null && endSegment == null && startIndexAndFlags == 0 && endIndexAndFlags == 0));

        _startObject = startSegment;
        _endObject = endSegment;
        _startInteger = startIndexAndFlags;
        _endInteger = endIndexAndFlags;
    }

    /// <summary>
    /// Creates an instance of <see cref="ReadOnlySequence{T}"/> from linked memory list represented by start and end segments
    /// and corresponding indexes in them.
    /// </summary>
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
        _endInteger = ReadOnlySequence.ArrayToSequenceEnd(array.Length);
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
        _endInteger = ReadOnlySequence.ArrayToSequenceEnd(start + length);
    }

    public Sequence(ReadOnlyMemory<T> memory)
    {
        if (MemoryMarshal.TryGetMemoryManager(memory, out MemoryManager<T>? manager, out int index, out int length))
        {
            _startObject = manager;
            _endObject = manager;
            _startInteger = ReadOnlySequence.MemoryManagerToSequenceStart(index);
            _endInteger = index + length;
        }
        else if (MemoryMarshal.TryGetArray(memory, out ArraySegment<T> segment))
        {
            T[]? array = segment.Array;
            int start = segment.Offset;
            _startObject = array;
            _endObject = array;
            _startInteger = start;
            _endInteger = ReadOnlySequence.ArrayToSequenceEnd(start + segment.Count);
        }
        else
        {
            throw new ArgumentException();
        }
    }

    public ReadOnlySequence<T> Slice(long start, long length)
    {
        if (start < 0 || length < 0)
            ThrowHelper.ThrowStartOrEndArgumentValidationException(start);

        SequencePosition begin;
        SequencePosition end;

        int startIndex = GetIndex(_startInteger);
        int endIndex = GetIndex(_endInteger);

        object? startObject = _startObject;
        object? endObject = _endObject;

        if (startObject != endObject)
        {
            Debug.Assert(startObject != null);
            var startSegment = (ReadOnlySequenceSegment<T>)startObject;

            int currentLength = startSegment.Memory.Length - startIndex;

            // Position in start segment
            if (currentLength > start)
            {
                startIndex += (int)start;
                begin = new SequencePosition(startObject, startIndex);

                end = GetEndPosition(startSegment, startObject, startIndex, endObject!, endIndex, length);
            }
            else
            {
                if (currentLength < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();

                begin = SeekMultiSegment(startSegment.Next!, endObject!, endIndex, start - currentLength, ExceptionArgument.start);

                int beginIndex = begin.GetInteger();
                object beginObject = begin.GetObject()!;

                if (beginObject != endObject)
                {
                    Debug.Assert(beginObject != null);
                    end = GetEndPosition((ReadOnlySequenceSegment<T>)beginObject, beginObject, beginIndex, endObject!, endIndex, length);
                }
                else
                {
                    if (endIndex - beginIndex < length)
                        ThrowHelper.ThrowStartOrEndArgumentValidationException(0);  // Passing value >= 0 means throw exception on length argument

                    end = new SequencePosition(beginObject, beginIndex + (int)length);
                }
            }
        }
        else
        {
            if (endIndex - startIndex < start)
                ThrowHelper.ThrowStartOrEndArgumentValidationException(-1); // Passing value < 0 means throw exception on start argument

            startIndex += (int)start;
            begin = new SequencePosition(startObject, startIndex);

            if (endIndex - startIndex < length)
                ThrowHelper.ThrowStartOrEndArgumentValidationException(0);  // Passing value >= 0 means throw exception on length argument

            end = new SequencePosition(startObject, startIndex + (int)length);
        }

        return SliceImpl(begin, end);
    }

    public ReadOnlySequence<T> Slice(long start, SequencePosition end)
    {
        if (start < 0)
            ThrowHelper.ThrowStartOrEndArgumentValidationException(start);

        uint startIndex = (uint)GetIndex(_startInteger);
        object? startObject = _startObject;

        uint endIndex = (uint)GetIndex(_endInteger);
        object? endObject = _endObject;

        uint sliceEndIndex = (uint)end.GetInteger();
        object? sliceEndObject = end.GetObject();

        if (sliceEndObject == null)
        {
            sliceEndObject = _startObject;
            sliceEndIndex = startIndex;
        }

        // Single-Segment Sequence
        if (startObject == endObject)
        {
            if (startObject != sliceEndObject || !InRange(sliceEndIndex, startIndex, endIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
            }

            if (sliceEndIndex - startIndex < start)
                ThrowHelper.ThrowStartOrEndArgumentValidationException(-1); // Passing value < 0 means throw exception on start argument

            goto FoundInFirstSegment;
        }

        // Multi-Segment Sequence
        var startSegment = (ReadOnlySequenceSegment<T>)startObject!;
        ulong startRange = (ulong)(startSegment.RunningIndex + startIndex);
        ulong sliceRange = (ulong)(((ReadOnlySequenceSegment<T>)sliceEndObject!).RunningIndex + sliceEndIndex);

        // This optimization works because we know sliceEndIndex, startIndex, and endIndex are all >= 0
        Debug.Assert(sliceEndIndex >= 0 && startIndex >= 0 && endIndex >= 0);
        if (!InRange(
            sliceRange,
            startRange,
            (ulong)(((ReadOnlySequenceSegment<T>)endObject!).RunningIndex + endIndex)))
        {
            ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
        }

        if (startRange + (ulong)start > sliceRange)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
        }

        int currentLength = startSegment.Memory.Length - (int)startIndex;

        // Position not in startSegment
        if (currentLength <= start)
        {
            if (currentLength < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();

            // End of segment. Move to start of next.
            SequencePosition begin = SeekMultiSegment(startSegment.Next!, sliceEndObject, (int)sliceEndIndex, start - currentLength, ExceptionArgument.start);
            return SliceImpl(begin, end);
        }

    FoundInFirstSegment:
        // startIndex + start <= int.MaxValue
        Debug.Assert(start <= int.MaxValue - startIndex);
        return SliceImpl(new SequencePosition(startObject, (int)startIndex + (int)start), new SequencePosition(sliceEndObject, (int)sliceEndIndex));
    }

    /// <summary>
    /// Forms a slice out of the current <see cref="ReadOnlySequence{T}"/>, beginning at <paramref name="start"/>, with <paramref name="length"/> items.
    /// </summary>
    /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
    /// <param name="length">The length of the slice.</param>
    /// <returns>A slice that consists of <paramref name="length" /> elements from the current instance starting at sequence position <paramref name="start" />.</returns>
    public ReadOnlySequence<T> Slice(SequencePosition start, long length)
    {
        uint startIndex = (uint)GetIndex(_startInteger);
        object? startObject = _startObject;

        uint endIndex = (uint)GetIndex(_endInteger);
        object? endObject = _endObject;

        // Check start before length
        uint sliceStartIndex = (uint)start.GetInteger();
        object? sliceStartObject = start.GetObject();

        if (sliceStartObject == null)
        {
            sliceStartIndex = startIndex;
            sliceStartObject = _startObject;
        }

        // Single-Segment Sequence
        if (startObject == endObject)
        {
            if (startObject != sliceStartObject || !InRange(sliceStartIndex, startIndex, endIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
            }

            if (length < 0)
                // Passing value >= 0 means throw exception on length argument
                ThrowHelper.ThrowStartOrEndArgumentValidationException(0);

            if (endIndex - sliceStartIndex < length)
                ThrowHelper.ThrowStartOrEndArgumentValidationException(0);

            goto FoundInFirstSegment;
        }

        // Multi-Segment Sequence
        var sliceStartSegment = (ReadOnlySequenceSegment<T>)sliceStartObject!;
        ulong sliceRange = (ulong)((sliceStartSegment.RunningIndex + sliceStartIndex));
        ulong startRange = (ulong)(((ReadOnlySequenceSegment<T>)startObject!).RunningIndex + startIndex);
        ulong endRange = (ulong)(((ReadOnlySequenceSegment<T>)endObject!).RunningIndex + endIndex);

        // This optimization works because we know sliceStartIndex, startIndex, and endIndex are all >= 0
        Debug.Assert(sliceStartIndex >= 0 && startIndex >= 0 && endIndex >= 0);
        if (!InRange(sliceRange, startRange, endRange))
        {
            ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
        }

        if (length < 0)
            // Passing value >= 0 means throw exception on length argument
            ThrowHelper.ThrowStartOrEndArgumentValidationException(0);

        if (sliceRange + (ulong)length > endRange)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
        }

        int currentLength = sliceStartSegment.Memory.Length - (int)sliceStartIndex;

        // Position not in startSegment
        if (currentLength < length)
        {
            if (currentLength < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();

            // End of segment. Move to start of next.
            SequencePosition end = SeekMultiSegment(sliceStartSegment.Next!, endObject, (int)endIndex, length - currentLength, ExceptionArgument.length);
            return SliceImpl(start, end);
        }

    FoundInFirstSegment:
        // sliceStartIndex + length <= int.MaxValue
        Debug.Assert(length <= int.MaxValue - sliceStartIndex);
        return SliceImpl(new SequencePosition(sliceStartObject, (int)sliceStartIndex), new SequencePosition(sliceStartObject, (int)sliceStartIndex + (int)length));
    }

    public Sequence<T> Slice(int start, int length) => Slice((long)start, length);

    public Sequence<T> Slice(int start, SequencePosition end) => Slice((long)start, end);

    public Sequence<T> Slice(SequencePosition start, int length) => Slice(start, (long)length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Sequence<T> Slice(SequencePosition start, SequencePosition end)
    {
        BoundsCheck((uint)start.GetInteger(), start.GetObject(), (uint)end.GetInteger(), end.GetObject());
        return SliceImpl(start, end);
    }

    /// <summary>
    /// Forms a slice out of the current <see cref="ReadOnlySequence{T}" />, beginning at a specified sequence position and continuing to the end of the read-only sequence.
    /// </summary>
    /// <param name="start">The starting (inclusive) <see cref="SequencePosition"/> at which to begin this slice.</param>
    /// <returns>A slice starting at sequence position <paramref name="start" /> and continuing to the end of the current read-only sequence.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySequence<T> Slice(SequencePosition start)
    {
        bool positionIsNotNull = start.GetObject() != null;
        BoundsCheck(start, positionIsNotNull);
        return SliceImpl(positionIsNotNull ? start : Start);
    }

    /// <summary>
    /// Forms a slice out of the current <see cref="ReadOnlySequence{T}" /> , beginning at a specified index and continuing to the end of the read-only sequence.
    /// </summary>
    /// <param name="start">The start index at which to begin this slice.</param>
    /// <returns>A slice starting at index <paramref name="start" /> and continuing to the end of the current read-only sequence.</returns>
    public ReadOnlySequence<T> Slice(long start)
    {
        if (start < 0)
            ThrowHelper.ThrowStartOrEndArgumentValidationException(start);

        if (start == 0)
            return this;

        SequencePosition begin = Seek(start, ExceptionArgument.start);
        return SliceImpl(begin);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (typeof(T) == typeof(char))
        {
            Sequence<T> localThis = this;
            Sequence<char> charSequence = Unsafe.As<Sequence<T>, Sequence<char>>(ref localThis);

            if (Length < int.MaxValue)
            {
                return string.Create((int)Length, charSequence, (span, sequence) => sequence.CopyTo(span));
            }
        }

        return $"System.Buffers.ReadOnlySequence<{typeof(T).Name}>[{Length}]";
    }

    /// <summary>
    /// Returns an enumerator over the <see cref="ReadOnlySequence{T}"/>
    /// </summary>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Returns a new <see cref="SequencePosition"/> at an <paramref name="offset"/> from the start of the sequence.
    /// </summary>
    public SequencePosition GetPosition(long offset)
    {
        if (offset < 0)
            ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();

        return Seek(offset);
    }

    /// <summary>
    /// Returns the offset of a <paramref name="position" /> within this sequence.
    /// </summary>
    /// <param name="position">The <see cref="System.SequencePosition"/> of which to get the offset.</param>
    /// <returns>The offset in the sequence.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The position is out of range.</exception>
    /// <remarks>
    /// The returned offset is not a zero-based index from the start.
    /// To obtain the zero-based index offset, subtract <code>mySequence.GetOffset(mySequence.Start)</code> from the returned offset.
    /// </remarks>
    public long GetOffset(SequencePosition position)
    {
        object? positionSequenceObject = position.GetObject();
        bool positionIsNull = positionSequenceObject == null;
        BoundsCheck(position, !positionIsNull);

        object? startObject = _startObject;
        object? endObject = _endObject;

        uint positionIndex = (uint)position.GetInteger();

        // if sequence object is null we suppose start segment
        if (positionIsNull)
        {
            positionSequenceObject = _startObject;
            positionIndex = (uint)GetIndex(_startInteger);
        }

        // Single-Segment Sequence
        if (startObject == endObject)
        {
            return positionIndex;
        }
        else
        {
            // Verify position validity, this is not covered by BoundsCheck for Multi-Segment Sequence
            // BoundsCheck for Multi-Segment Sequence check only validity inside current sequence but not for SequencePosition validity.
            // For single segment position bound check is implicit.
            Debug.Assert(positionSequenceObject != null);

            if (((ReadOnlySequenceSegment<T>)positionSequenceObject).Memory.Length - positionIndex < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();

            // Multi-Segment Sequence
            ReadOnlySequenceSegment<T>? currentSegment = (ReadOnlySequenceSegment<T>?)startObject;
            while (currentSegment != null && currentSegment != positionSequenceObject)
            {
                currentSegment = currentSegment.Next!;
            }

            // Hit the end of the segments but didn't find the segment
            if (currentSegment is null)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
            }

            Debug.Assert(currentSegment!.RunningIndex + positionIndex >= 0);

            return currentSegment!.RunningIndex + positionIndex;
        }
    }

    /// <summary>
    /// Returns a new <see cref="SequencePosition"/> at an <paramref name="offset"/> from the <paramref name="origin"/>
    /// </summary>
    public SequencePosition GetPosition(long offset, SequencePosition origin)
    {
        if (offset < 0)
            ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();

        return Seek(origin, offset);
    }

    public bool TryGet(ref SequencePosition position, out Memory<T> memory, bool advance = true)
    {
        bool result = TryGetBuffer(position, out memory, out SequencePosition next);
        if (advance)
        {
            position = next;
        }

        return result;
    }

    public struct Enumerator
    {
        private readonly Sequence<T> _sequence;
        private SequencePosition _next;
        private Memory<T> _currentMemory;

        public Enumerator(in Sequence<T> sequence)
        {
            _currentMemory = default;
            _next = sequence.Start;
            _sequence = sequence;
        }

        public Memory<T> Current => _currentMemory;

        public bool MoveNext()
        {
            if (_next.GetObject() == null)
            {
                return false;
            }

            return _sequence.TryGet(ref _next, out _currentMemory);
        }
    }

    private enum SequenceType
    {
        MultiSegment = 0x00,
        Array = 0x1,
        MemoryManager = 0x2,
        String = 0x3,
    }
}

internal static class Sequence
{
    public const int FlagBitMask = 1 << 31;
    public const int IndexBitMask = ~FlagBitMask;

    public const int ArrayEndMask = FlagBitMask;

    public const int MemoryManagerStartMask = FlagBitMask;

    public const int StringStartMask = FlagBitMask;
    public const int StringEndMask = FlagBitMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ArrayToSequenceEnd(int endIndex) => endIndex | ArrayEndMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MemoryManagerToSequenceStart(int startIndex) => startIndex | MemoryManagerStartMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int StringToSequenceStart(int startIndex) => startIndex | StringStartMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int StringToSequenceEnd(int endIndex) => endIndex | StringEndMask;
}
*/