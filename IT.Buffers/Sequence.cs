using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IT.Buffers;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class Sequence<T> : IBufferWriter<T>, IDisposable
{
    private const int MaximumAutoGrowSize = 32 * 1024;

    private static readonly int DefaultLengthFromArrayPool = 1 + (4095 / Unsafe.SizeOf<T>());

    private static readonly ReadOnlySequence<T> Empty = new(Segment.Empty, 0, Segment.Empty, 0);

    public static BufferPool<Sequence<T>> Pool => BufferPool<Sequence<T>>.Shared;

    private readonly Stack<Segment> _stack;

    private ArrayPool<T>? _arrayPool;

    private Segment? _first;

    private Segment? _last;

    public Sequence()
    {
        _stack = new();
    }

    private string DebuggerDisplay => $"Length: {AsReadOnlySequence.Length}";

#if NET
    public int EnsureCapacitySegments(int capacity)
    {
        return _stack.EnsureCapacity(capacity);
    }
#endif

    public ArrayPool<T>? ArrayPool
    {
        get => _arrayPool;
        set
        {
            if (_last != null) throw new InvalidOperationException();

            _arrayPool = value;
        }
    }

    public int MinimumSpanLength { get; set; } = 0;

    public bool AutoIncreaseMinimumSpanLength { get; set; } = true;

    public ReadOnlySequence<T> AsReadOnlySequence => this;

    public long Length => AsReadOnlySequence.Length;


    public static implicit operator ReadOnlySequence<T>(Sequence<T>? sequence)
    {
        return sequence?._first is { } first && sequence._last is { } last
            ? new ReadOnlySequence<T>(first, first.Start, last, last.End)
            : Empty;
    }

    public void AdvanceTo(SequencePosition position)
    {
        var firstSegment = (Segment?)position.GetObject();
        if (firstSegment == null)
        {
            // Emulate PipeReader behavior which is to just return for default(SequencePosition)
            return;
        }

        if (ReferenceEquals(firstSegment, Segment.Empty) && Length == 0)
        {
            // We were called with our own empty buffer segment.
            return;
        }

        int firstIndex = position.GetInteger();

        // Before making any mutations, confirm that the block specified belongs to this sequence.
        var current = _first;
        while (current != firstSegment && current != null)
        {
            current = current.Next;
        }

        if (current == null)
            throw new ArgumentException("Position does not represent a valid position in this sequence.", nameof(position));

        // Also confirm that the position is not a prior position in the block.
        if (firstIndex < current.Start)
            throw new ArgumentException("Position must not be earlier than current position.", nameof(position));

        // Now repeat the loop, performing the mutations.
        current = _first;
        while (current != firstSegment)
        {
            current = RecycleAndGetNext(current!);
        }

        firstSegment.AdvanceTo(firstIndex);

        _first = firstSegment.Length == 0 ? RecycleAndGetNext(firstSegment) : firstSegment;

        if (_first == null)
        {
            _last = null;
        }
    }

    public void Advance(int count)
    {
        Segment? last = _last ?? throw new InvalidOperationException();
        last.Advance(count);
        ConsiderMinimumSizeIncrease();
    }

    public Memory<T> GetMemory(int sizeHint) => GetSegment(sizeHint).RemainingMemory;

    public Span<T> GetSpan(int sizeHint) => GetSegment(sizeHint).RemainingSpan;

    public void Append(ReadOnlyMemory<T> memory)
    {
        if (memory.Length > 0)
        {
            var segment = GetOrNewSegment();
            segment.AssignForeign(memory);
            Append(segment);
        }
    }

    public void Reset()
    {
        var current = _first;
        while (current != null)
        {
            current = RecycleAndGetNext(current);
        }

        _first = _last = null;
        _arrayPool = null;
    }

    private Segment GetSegment(int sizeHint)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        int? minBufferSize = null;
        if (sizeHint == 0)
        {
            if (_last == null || _last.WritableBytes == 0)
            {
                // We're going to need more memory. Take whatever size the pool wants to give us.
                minBufferSize = -1;
            }
        }
        else
        {
            if (_last == null || _last.WritableBytes < sizeHint)
            {
                minBufferSize = Math.Max(MinimumSpanLength, sizeHint);
            }
        }

        if (minBufferSize.HasValue)
        {
            Segment? segment = GetOrNewSegment();
            
            segment.Assign((_arrayPool ?? ArrayPool<T>.Shared).Rent(minBufferSize.Value == -1 ? DefaultLengthFromArrayPool : minBufferSize.Value));
            Append(segment);
        }

        return _last!;
    }

    private Segment GetOrNewSegment()
    {
        if (!_stack.TryPop(out var segment))
        {
            segment = new Segment();
        }
        return segment;
    }

    private void Append(Segment segment)
    {
        if (_last == null)
        {
            _first = _last = segment;
        }
        else
        {
            if (_last.Length > 0)
            {
                // Add a new block.
                _last.SetNext(segment);
            }
            else
            {
                // The last block is completely unused. Replace it instead of appending to it.
                Segment? current = _first;
                if (_first != _last)
                {
                    while (current!.Next != _last)
                    {
                        current = current.Next;
                    }
                }
                else
                {
                    _first = segment;
                }

                current!.SetNext(segment);
                RecycleAndGetNext(_last);
            }

            _last = segment;
        }
    }

    private Segment? RecycleAndGetNext(Segment segment)
    {
        Segment? recycledSegment = segment;
        Segment? nextSegment = segment.Next;
        recycledSegment.ResetMemory(_arrayPool);
        _stack.Push(recycledSegment);
        return nextSegment;
    }

    private void ConsiderMinimumSizeIncrease()
    {
        if (AutoIncreaseMinimumSpanLength && MinimumSpanLength < MaximumAutoGrowSize)
        {
            int autoSize = Math.Min(MaximumAutoGrowSize, (int)Math.Min(int.MaxValue, Length / 2));
            if (MinimumSpanLength < autoSize)
            {
                MinimumSpanLength = autoSize;
            }
        }
    }

    private class Segment : ReadOnlySequenceSegment<T>
    {
        internal static readonly Segment Empty = new();

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
        /// <summary>
        /// Gets the backing array, when using an <see cref="ArrayPool{T}"/> instead of a <see cref="MemoryPool{T}"/>.
        /// </summary>
        private T[]? _array;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly

        /// <summary>
        /// Gets the position within <see cref="ReadOnlySequenceSegment{T}.Memory"/> where the data starts.
        /// </summary>
        /// <remarks>This may be nonzero as a result of calling <see cref="Sequence{T}.AdvanceTo(SequencePosition)"/>.</remarks>
        internal int Start { get; private set; }

        /// <summary>
        /// Gets the position within <see cref="ReadOnlySequenceSegment{T}.Memory"/> where the data ends.
        /// </summary>
        internal int End { get; private set; }

        /// <summary>
        /// Gets the tail of memory that has not yet been committed.
        /// </summary>
        internal Memory<T> RemainingMemory => AvailableMemory.Slice(End);

        /// <summary>
        /// Gets the tail of memory that has not yet been committed.
        /// </summary>
        internal Span<T> RemainingSpan => AvailableMemory.Span.Slice(End);

        /// <summary>
        /// Gets the full memory owned by the <see cref="MemoryOwner"/>.
        /// </summary>
        internal Memory<T> AvailableMemory => _array ?? default;

        /// <summary>
        /// Gets the number of elements that are committed in this segment.
        /// </summary>
        internal int Length => End - Start;

        /// <summary>
        /// Gets the amount of writable bytes in this segment.
        /// It is the amount of bytes between <see cref="Length"/> and <see cref="End"/>.
        /// </summary>
        internal int WritableBytes => AvailableMemory.Length - End;

        internal new Segment? Next
        {
            get => (Segment?)base.Next;
            set => base.Next = value;
        }

        /// <summary>
        /// Gets a value indicating whether this segment refers to memory that came from outside and that we cannot write to nor recycle.
        /// </summary>
        internal bool IsForeignMemory => _array == null;

        internal void Assign(T[] array)
        {
            _array = array;
            Memory = array;
        }

        internal void AssignForeign(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
            End = memory.Length;
        }

        internal void ResetMemory(ArrayPool<T>? arrayPool)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                AvailableMemory.Span.Slice(Start, End - Start).Clear();
            }
            Memory = default;
            Next = null;
            RunningIndex = 0;
            Start = 0;
            End = 0;
            var array = _array;
            if (array != null)
            {
                (arrayPool ?? ArrayPool<T>.Shared).Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                _array = null;
            }
        }

        internal void SetNext(Segment segment)
        {
            Next = segment;
            segment.RunningIndex = RunningIndex + Start + Length;

            // Trim any slack on this segment.
            if (!IsForeignMemory)
            {
                // When setting Memory, we start with index 0 instead of Start because
                // the first segment has an explicit index set anyway,
                // and we don't want to double-count it here.
                Memory = AvailableMemory.Slice(0, Start + Length);
            }
        }

        internal void Advance(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            var end = End + count;
            if (end > Memory.Length) throw new ArgumentOutOfRangeException(nameof(count));

            End = end;
        }

        internal void AdvanceTo(int offset)
        {
            Debug.Assert(offset >= Start, "Trying to rewind.");
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                AvailableMemory.Span.Slice(Start, offset - Start).Clear();
            }
            Start = offset;
        }
    }

    void IDisposable.Dispose() => Reset();
}