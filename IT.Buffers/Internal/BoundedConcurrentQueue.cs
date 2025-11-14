using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace IT.Buffers.Internal;

//https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Concurrent/ConcurrentQueueSegment.cs
[DebuggerDisplay("Capacity = {Capacity}")]
internal sealed class BoundedConcurrentQueue<T>
{
    internal readonly Slot[] _slots; // SOS's ThreadPool command depends on this name
    internal readonly int _slotsMask;
    internal PaddedHeadAndTail _headAndTail;
    internal bool _preservedForObservation;
    internal bool _frozenForEnqueues;
#pragma warning disable 0649 // some builds don't assign to this field
    /// <summary>The segment following this one in the queue, or null if this segment is the last in the queue.</summary>
    internal BoundedConcurrentQueue<T>? _nextSegment; // SOS's ThreadPool command depends on this name
#pragma warning restore 0649

    /// <param name="boundedLength">
    /// The maximum number of elements the segment can contain.  Must be a power of 2.
    /// </param>
    internal BoundedConcurrentQueue(int power2 = 5)
    {
        if (power2 < 1 || power2 > 30) throw new System.ArgumentOutOfRangeException(nameof(power2));
        
        var boundedLength = 2 << (power2 - 1);

        Debug.Assert(boundedLength >= 2, $"Must be >= 2, got {boundedLength}");
#if NET
        Debug.Assert(System.Numerics.BitOperations.IsPow2(boundedLength), $"Must be a power of 2, got {boundedLength}");
#endif
        _slots = new Slot[boundedLength];
        _slotsMask = boundedLength - 1;

        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].SequenceNumber = i;
        }
    }

    /// <summary>Gets the number of elements this segment can store.</summary>
    internal int Capacity => _slots.Length;

    /// <summary>Gets the "freeze offset" for this segment.</summary>
    internal int FreezeOffset => _slots.Length * 2;

    internal void EnsureFrozenForEnqueues() // must only be called while queue's segment lock is held
    {
        if (!_frozenForEnqueues) // flag used to ensure we don't increase the Tail more than once if frozen more than once
        {
            _frozenForEnqueues = true;
            Interlocked.Add(ref _headAndTail.Tail, FreezeOffset);
        }
    }

    /// <summary>Tries to dequeue an element from the queue.</summary>
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        Slot[] slots = _slots;
        SpinWait spinner = default;
        while (true)
        {
            int currentHead = Volatile.Read(ref _headAndTail.Head);
            int slotsIndex = currentHead & _slotsMask;
            int sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);
            int diff = sequenceNumber - (currentHead + 1);
            if (diff == 0)
            {
                if (Interlocked.CompareExchange(ref _headAndTail.Head, currentHead + 1, currentHead) == currentHead)
                {
                    item = slots[slotsIndex].Item!;
                    if (!Volatile.Read(ref _preservedForObservation))
                    {
                        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                        {
                            slots[slotsIndex].Item = default;
                        }
                        Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentHead + slots.Length);
                    }
                    return true;
                }

            }
            else if (diff < 0)
            {
                bool frozen = _frozenForEnqueues;
                int currentTail = Volatile.Read(ref _headAndTail.Tail);
                if (currentTail - currentHead <= 0 || (frozen && (currentTail - FreezeOffset - currentHead <= 0)))
                {
                    item = default;
                    return false;
                }

                spinner.SpinOnce(
#if NET
                    sleep1Threshold: -1
#endif
                    );
            }
        }
    }

    /// <summary>Tries to peek at an element from the queue, without removing it.</summary>
    public bool TryPeek([MaybeNullWhen(false)] out T result, bool resultUsed)
    {
        if (resultUsed)
        {
            _preservedForObservation = true;
            Interlocked.MemoryBarrier();
        }

        Slot[] slots = _slots;

        SpinWait spinner = default;
        while (true)
        {
            int currentHead = Volatile.Read(ref _headAndTail.Head);
            int slotsIndex = currentHead & _slotsMask;
            int sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);
            int diff = sequenceNumber - (currentHead + 1);
            if (diff == 0)
            {
                result = resultUsed ? slots[slotsIndex].Item! : default!;
                return true;
            }
            else if (diff < 0)
            {
                bool frozen = _frozenForEnqueues;
                int currentTail = Volatile.Read(ref _headAndTail.Tail);
                if (currentTail - currentHead <= 0 || (frozen && (currentTail - FreezeOffset - currentHead <= 0)))
                {
                    result = default;
                    return false;
                }

                spinner.SpinOnce(
#if NET
                    sleep1Threshold: -1
#endif
                    );
            }
        }
    }

    /// <summary>
    /// Attempts to enqueue the item.  If successful, the item will be stored
    /// in the queue and true will be returned; otherwise, the item won't be stored, and false
    /// will be returned.
    /// </summary>
    public bool TryEnqueue(T item)
    {
        Slot[] slots = _slots;
        while (true)
        {
            int currentTail = Volatile.Read(ref _headAndTail.Tail);
            int slotsIndex = currentTail & _slotsMask;
            int sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);
            int diff = sequenceNumber - currentTail;
            if (diff == 0)
            {
                if (Interlocked.CompareExchange(ref _headAndTail.Tail, currentTail + 1, currentTail) == currentTail)
                {
                    slots[slotsIndex].Item = item;
                    Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentTail + 1);
                    return true;
                }
            }
            else if (diff < 0)
            {
                return false;
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("Item = {Item}, SequenceNumber = {SequenceNumber}")]
    internal struct Slot
    {
        public T? Item;
        public int SequenceNumber;
    }
}

//https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/Internal/Padding.cs
internal class PaddingHelpers
{
#if TARGET_ARM64 || TARGET_LOONGARCH64
    internal const int CACHE_LINE_SIZE = 128;
#else
    internal const int CACHE_LINE_SIZE = 64;
#endif
}

[DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
[StructLayout(LayoutKind.Explicit, Size = 3 * PaddingHelpers.CACHE_LINE_SIZE)]
internal struct PaddedHeadAndTail
{
    [FieldOffset(1 * PaddingHelpers.CACHE_LINE_SIZE)] public int Head;
    [FieldOffset(2 * PaddingHelpers.CACHE_LINE_SIZE)] public int Tail;
}