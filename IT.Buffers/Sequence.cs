using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IT.Buffers;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class Sequence<T> : IBufferWriter<T>, IDisposable
{
    private const int MaximumAutoGrowSize = 32 * 1024;

    private static readonly int DefaultLengthFromArrayPool = 1 + (4095 / Unsafe.SizeOf<T>());

    private static readonly ReadOnlySequence<T> Empty = new(SequenceSegment.Empty, 0, SequenceSegment.Empty, 0);

    private readonly Stack<SequenceSegment> _segmentPool = new();

    private readonly MemoryPool<T>? _memoryPool;

    private readonly ArrayPool<T>? _arrayPool;

    private SequenceSegment? _first;

    private SequenceSegment? _last;

    public Sequence()
        : this(ArrayPool<T>.Create())
    {
    }

    public Sequence(MemoryPool<T> memoryPool)
    {
        Requires.NotNull(memoryPool, nameof(memoryPool));
        _memoryPool = memoryPool;
    }

    public Sequence(ArrayPool<T> arrayPool)
    {
        Requires.NotNull(arrayPool, nameof(arrayPool));
        _arrayPool = arrayPool;
    }

    public int MinimumSpanLength { get; set; } = 0;

    public bool AutoIncreaseMinimumSpanLength { get; set; } = true;

    public ReadOnlySequence<T> AsReadOnlySequence => this;

    public long Length => AsReadOnlySequence.Length;

    private string DebuggerDisplay => $"Length: {AsReadOnlySequence.Length}";

    public static implicit operator ReadOnlySequence<T>(Sequence<T>? sequence)
    {
        return sequence?._first is { } first && sequence._last is { } last
            ? new ReadOnlySequence<T>(first, first.Start, last, last.End)
            : Empty;
    }

    public void AdvanceTo(SequencePosition position)
    {
        var firstSegment = (SequenceSegment?)position.GetObject();
        if (firstSegment == null)
        {
            // Emulate PipeReader behavior which is to just return for default(SequencePosition)
            return;
        }

        if (ReferenceEquals(firstSegment, SequenceSegment.Empty) && Length == 0)
        {
            // We were called with our own empty buffer segment.
            return;
        }

        int firstIndex = position.GetInteger();

        // Before making any mutations, confirm that the block specified belongs to this sequence.
        SequenceSegment? current = this._first;
        while (current != firstSegment && current != null)
        {
            current = current.Next;
        }

        Requires.Argument(current != null, nameof(position), "Position does not represent a valid position in this sequence.");

        // Also confirm that the position is not a prior position in the block.
        Requires.Argument(firstIndex >= current.Start, nameof(position), "Position must not be earlier than current position.");

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
        SequenceSegment? last = _last ?? throw new InvalidOperationException();
        last.Advance(count);
        ConsiderMinimumSizeIncrease();
    }

    public Memory<T> GetMemory(int sizeHint) => GetSegment(sizeHint).RemainingMemory;

    public Span<T> GetSpan(int sizeHint) => GetSegment(sizeHint).RemainingSpan;

    public void Append(ReadOnlyMemory<T> memory)
    {
        if (memory.Length > 0)
        {
            SequenceSegment? segment = _segmentPool.Count > 0 ? _segmentPool.Pop() : new SequenceSegment();
            segment.AssignForeign(memory);
            Append(segment);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Dispose() => Reset();

    public void Reset()
    {
        SequenceSegment? current = _first;
        while (current != null)
        {
            current = RecycleAndGetNext(current);
        }

        _first = _last = null;
    }

    private SequenceSegment GetSegment(int sizeHint)
    {
        Requires.Range(sizeHint >= 0, nameof(sizeHint));
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
            SequenceSegment? segment = _segmentPool.Count > 0 ? _segmentPool.Pop() : new SequenceSegment();
            if (_arrayPool != null)
            {
                segment.Assign(_arrayPool.Rent(minBufferSize.Value == -1 ? DefaultLengthFromArrayPool : minBufferSize.Value));
            }
            else
            {
                segment.Assign(_memoryPool!.Rent(minBufferSize.Value));
            }

            Append(segment);
        }

        return _last!;
    }

    private void Append(SequenceSegment segment)
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
                SequenceSegment? current = _first;
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

    private SequenceSegment? RecycleAndGetNext(SequenceSegment segment)
    {
        SequenceSegment? recycledSegment = segment;
        SequenceSegment? nextSegment = segment.Next;
        recycledSegment.ResetMemory(_arrayPool);
        _segmentPool.Push(recycledSegment);
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

    private class SequenceSegment : ReadOnlySequenceSegment<T>
    {
        internal static readonly SequenceSegment Empty = new();

        /// <summary>
        /// A value indicating whether the element may contain references (and thus must be cleared).
        /// </summary>
        private static readonly bool MayContainReferences = !typeof(T).GetTypeInfo().IsPrimitive;

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
        /// Gets the tracker for the underlying array for this segment, which can be used to recycle the array when we're disposed of.
        /// Will be <see langword="null"/> if using an array pool, in which case the memory is held by <see cref="_array"/>.
        /// </summary>
        internal IMemoryOwner<T>? MemoryOwner { get; private set; }

        /// <summary>
        /// Gets the full memory owned by the <see cref="MemoryOwner"/>.
        /// </summary>
        internal Memory<T> AvailableMemory => _array ?? MemoryOwner?.Memory ?? default;

        /// <summary>
        /// Gets the number of elements that are committed in this segment.
        /// </summary>
        internal int Length => End - Start;

        /// <summary>
        /// Gets the amount of writable bytes in this segment.
        /// It is the amount of bytes between <see cref="Length"/> and <see cref="End"/>.
        /// </summary>
        internal int WritableBytes => AvailableMemory.Length - End;

        internal new SequenceSegment? Next
        {
            get => (SequenceSegment?)base.Next;
            set => base.Next = value;
        }

        /// <summary>
        /// Gets a value indicating whether this segment refers to memory that came from outside and that we cannot write to nor recycle.
        /// </summary>
        internal bool IsForeignMemory => _array == null && MemoryOwner == null;

        internal void Assign(IMemoryOwner<T> memoryOwner)
        {
            MemoryOwner = memoryOwner;
            Memory = memoryOwner.Memory;
        }

        internal void Assign(T[] array)
        {
            _array = array;
            Memory = array;
        }

        /// <summary>
        /// Assigns this (recyclable) segment a new area in memory.
        /// </summary>
        /// <param name="memory">A memory block obtained from outside, that we do not own and should not recycle.</param>
        internal void AssignForeign(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
            End = memory.Length;
        }

        internal void ResetMemory(ArrayPool<T>? arrayPool)
        {
            ClearReferences(Start, End - Start);
            Memory = default;
            Next = null;
            RunningIndex = 0;
            Start = 0;
            End = 0;
            if (_array != null)
            {
                arrayPool!.Return(_array);
                _array = null;
            }
            else
            {
                MemoryOwner?.Dispose();
                MemoryOwner = null;
            }
        }

        internal void SetNext(SequenceSegment segment)
        {
            Next = segment;
            segment.RunningIndex = RunningIndex + Start + Length;

            // Trim any slack on this segment.
            if (!IsForeignMemory)
            {
                // When setting Memory, we start with index 0 instead of this.Start because
                // the first segment has an explicit index set anyway,
                // and we don't want to double-count it here.
                Memory = AvailableMemory.Slice(0, Start + Length);
            }
        }

        /// <summary>
        /// Commits more elements as written in this segment.
        /// </summary>
        /// <param name="count">The number of elements written.</param>
        internal void Advance(int count)
        {
            Requires.Range(count >= 0 && End + count <= Memory.Length, nameof(count));
            End += count;
        }

        /// <summary>
        /// Removes some elements from the start of this segment.
        /// </summary>
        /// <param name="offset">The number of elements to ignore from the start of the underlying array.</param>
        internal void AdvanceTo(int offset)
        {
            Debug.Assert(offset >= Start, "Trying to rewind.");
            ClearReferences(Start, offset - Start);
            Start = offset;
        }

        private void ClearReferences(int startIndex, int length)
        {
            if (MayContainReferences)
            {
                AvailableMemory.Span.Slice(startIndex, length).Clear();
            }
        }
    }

    internal static class Requires
    {
        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if a condition does not evaluate to true.
        /// </summary>
        [DebuggerStepThrough]
        public static void Range([DoesNotReturnIf(false)] bool condition, string parameterName, string? message = null)
        {
            if (!condition)
            {
                FailRange(parameterName, message);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if a condition does not evaluate to true.
        /// </summary>
        /// <returns>Nothing.  This method always throws.</returns>
        [DebuggerStepThrough]
        public static Exception FailRange(string parameterName, string? message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
            else
            {
                throw new ArgumentOutOfRangeException(parameterName, message);
            }
        }

        /// <summary>
        /// Throws an exception if the specified parameter's value is null.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="value">The value of the argument.</param>
        /// <param name="parameterName">The name of the parameter to include in any thrown exception.</param>
        /// <returns>The value of the parameter.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
        [DebuggerStepThrough]
        public static T NotNull<T>([NotNull] T value, string parameterName)
            where T : class // ensures value-types aren't passed to a null checking method
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        /// <summary>
        /// Throws an ArgumentException if a condition does not evaluate to true.
        /// </summary>
        [DebuggerStepThrough]
        public static void Argument([DoesNotReturnIf(false)] bool condition, string parameterName, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// Throws an ArgumentException if a condition does not evaluate to true.
        /// </summary>
        [DebuggerStepThrough]
        public static void Argument([DoesNotReturnIf(false)] bool condition, string parameterName, string message, object arg1)
        {
            if (!condition)
            {
                throw new ArgumentException(String.Format(message, arg1), parameterName);
            }
        }

        /// <summary>
        /// Throws an ArgumentException if a condition does not evaluate to true.
        /// </summary>
        [DebuggerStepThrough]
        public static void Argument([DoesNotReturnIf(false)] bool condition, string parameterName, string message, object arg1, object arg2)
        {
            if (!condition)
            {
                throw new ArgumentException(String.Format(message, arg1, arg2), parameterName);
            }
        }

        /// <summary>
        /// Throws an ArgumentException if a condition does not evaluate to true.
        /// </summary>
        [DebuggerStepThrough]
        public static void Argument([DoesNotReturnIf(false)] bool condition, string parameterName, string message, params object[] args)
        {
            if (!condition)
            {
                throw new ArgumentException(String.Format(message, args), parameterName);
            }
        }
    }
}