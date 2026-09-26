using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace IT.Buffers;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class SequenceBufferWriter<T> : IBufferWriter<T>, IResetable//, ISequenceOwner<T>
{
    private static readonly ReadOnlySequence<T> Empty = new(Segment.Empty, 0, Segment.Empty, 0);

    public static BufferPool<SequenceBufferWriter<T>> Pool => 
        NewBufferPool<SequenceBufferWriter<T>>.Shared;

    private readonly Stack<Segment> _stack;
    private ArrayPool<T>? _arrayPool;
    private MemoryPool<T>? _memoryPool;
    private IBufferGrowthStrategy? _growthStrategy;
    private Segment? _first;
    private Segment? _last;
    private int _nextBufferSize;

    public SequenceBufferWriter()
    {
        _stack = new();
    }

    private string DebuggerDisplay => $"Length: {Sequence.Length}";

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

    public MemoryPool<T>? MemoryPool
    {
        get => _memoryPool;
        set => _memoryPool = value;
    }

    public IBufferGrowthStrategy? GrowthStrategy
    {
        get => _growthStrategy;
        set => _growthStrategy = value;
    }

    public int NextBufferSize
    {
        get { return _nextBufferSize; }
        set
        {
            if (value < 0 || value > BufferSize.Max) throw new ArgumentOutOfRangeException(nameof(value));
            _nextBufferSize = value;
        }
    }

    //TODO: change to Sequence<T>
    public ReadOnlySequence<T> Sequence
    {
        get
        {
            var last = _last;
            if (last != null)
            {
                var first = _first;
                if (first != null)
                {
                    return new(first, first.Start, last, last.End);
                }
            }
            return Empty;
        }
    }

    public SequencePosition Start => _first != null ? new(_first, _first.Start) : default;

    public SequencePosition End => _last != null ? new(_last, _last.End) : default;

    public long Length => Sequence.Length;

    public static implicit operator ReadOnlySequence<T>(SequenceBufferWriter<T>? sequence)
        => sequence == null ? Empty : sequence.Sequence;

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

        if (firstIndex < current.Start)
            throw new ArgumentException("Position must not be earlier than current position.", nameof(position));

        if (firstIndex > current.End)
            throw new ArgumentException("Position must not be more recent than current position.", nameof(position));

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
        if (_last == null) throw new ArgumentOutOfRangeException(nameof(count));

        _last.Advance(count);
    }

    public Memory<T> GetMemory(int sizeHint = 0) => GetSegment(sizeHint).FreeMemory;

    public Span<T> GetSpan(int sizeHint = 0) => GetSegment(sizeHint).FreeSpan;

    //TODO: Buffer<T>??
    public void Append(Memory<T> memory)
    {
        if (memory.Length > 0)
        {
            var segment = GetOrNewSegment();
            segment.AssignForeign(memory);
            Append(segment);
        }
    }

    public void Append(IMemoryOwner<T> memoryOwner)
    {
        if (memoryOwner == null) throw new ArgumentNullException(nameof(memoryOwner));

        var segment = GetOrNewSegment();
        segment.AssignMemoryOwner(memoryOwner);
        Append(segment);
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
        _memoryPool = null;
        _growthStrategy = null;
        _nextBufferSize = 0;
    }

    private Segment GetSegment(int sizeHint)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        if (sizeHint == 0) sizeHint = 1;

        if (_last == null || _last.FreeLength < sizeHint)
        {
            var growthStrategy = _growthStrategy ?? BufferGrowthStrategy.OneOfEachSize;
            var bufferSize = _nextBufferSize;
            if (bufferSize == 0)
                _nextBufferSize = bufferSize = growthStrategy.GetBufferSize<T>();

            if (bufferSize >= sizeHint)
            {
                _nextBufferSize = growthStrategy.Grow(bufferSize);
                sizeHint = bufferSize;
            }

            if (_memoryPool != null)
            {
                var memoryOwner = _memoryPool.Rent(sizeHint);
                var segment = GetOrNewSegment();
                segment.AssignMemoryOwner(memoryOwner);
                Append(segment);
            }
            else
            {
                var array = Rent(sizeHint);
                var segment = GetOrNewSegment();
                segment.AssignArray(array);
                Append(segment);
            }
        }

        return _last!;
    }

    private T[] Rent(int sizeHint)
    {
        return (_arrayPool ?? ArrayPool<T>.Shared).Rent(sizeHint);
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
                var current = _first;
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
        var nextSegment = segment.Next;
        segment.ResetBuffer(_arrayPool);
        _stack.Push(segment);
        return nextSegment;
    }

    private class Segment : SequenceSegment<T>
    {
        internal static readonly Segment Empty = new();

        //Array or IMemoryOwner
        private object? _buffer;

        internal int Start { get; private set; }

        internal int End { get; private set; }

        internal int Length => End - Start;

        internal int FreeLength => AvailableMemory.Length - End;

        internal Memory<T> FreeMemory => AvailableMemory.Slice(End);

        internal Span<T> FreeSpan => AvailableMemory.Span.Slice(End);

        internal Memory<T> AvailableMemory
        {
            get
            {
                var buffer = _buffer;
                if (buffer is T[] array) return array;
                if (buffer is IMemoryOwner<T> memoryOwner) return memoryOwner.Memory;
                if (buffer == null) return default;

                throw new InvalidOperationException("buffer is unknown.");
            }
        }

        internal new Segment? Next
        {
            get => (Segment?)base.Next;
            set => base.Next = value;
        }

        internal bool IsForeignMemory => _buffer == null;

        internal void AssignArray(T[] array)
        {
            _buffer = array;
            Memory = array;
        }

        internal void AssignMemoryOwner(IMemoryOwner<T> memoryOwner)
        {
            _buffer = memoryOwner;
            Memory = memoryOwner.Memory;
        }

        internal void AssignForeign(Memory<T> memory)
        {
            Memory = memory;
            End = memory.Length;
        }

        internal void ResetBuffer(ArrayPool<T>? arrayPool)
        {
            Reset();
            Start = 0;
            End = 0;
            var buffer = Interlocked.Exchange(ref _buffer, null);
            if (buffer != null)
            {
                if (buffer is T[] array)
                {
                    (arrayPool ?? ArrayPool<T>.Shared).Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                }
                else
                {
                    ((IMemoryOwner<T>)buffer).Dispose();
                }
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
            if ((uint)end > Memory.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            End = end;
        }

        internal void AdvanceTo(int offset)
        {
            Debug.Assert(offset >= Start);
            Debug.Assert(offset <= End);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                AvailableMemory.Span.Slice(Start, offset - Start).Clear();
            }
            Start = offset;
        }
    }
}