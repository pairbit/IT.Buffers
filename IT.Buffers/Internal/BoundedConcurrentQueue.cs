using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace IT.Buffers.Internal;

[DebuggerDisplay("Capacity = {Capacity}")]
internal sealed class BoundedConcurrentQueue<T>
{
    // Segment design is inspired by the algorithm outlined at:
    // http://www.1024cores.net/home/lock-free-algorithms/queues/bounded-mpmc-queue

    /// <summary>The array of items in this queue.  Each slot contains the item in that slot and its "sequence number".</summary>
    internal readonly Slot[] _slots; // SOS's ThreadPool command depends on this name
    /// <summary>Mask for quickly accessing a position within the queue's array.</summary>
    internal readonly int _slotsMask;
    /// <summary>The head and tail positions, with padding to help avoid false sharing contention.</summary>
    /// <remarks>Dequeuing happens from the head, enqueuing happens at the tail.</remarks>
    internal PaddedHeadAndTail _headAndTail; // mutable struct: do not make this readonly

    /// <summary>Indicates whether the segment has been marked such that dequeues don't overwrite the removed data.</summary>
    internal bool _preservedForObservation;
    /// <summary>Indicates whether the segment has been marked such that no additional items may be enqueued.</summary>
    internal bool _frozenForEnqueues;
#pragma warning disable 0649 // some builds don't assign to this field
    /// <summary>The segment following this one in the queue, or null if this segment is the last in the queue.</summary>
    internal BoundedConcurrentQueue<T>? _nextSegment; // SOS's ThreadPool command depends on this name
#pragma warning restore 0649

    /// <summary>Creates the segment.</summary>
    /// <param name="boundedLength">
    /// The maximum number of elements the segment can contain.  Must be a power of 2.
    /// </param>
    internal BoundedConcurrentQueue(int boundedLength)
    {
        Debug.Assert(boundedLength >= 2, $"Must be >= 2, got {boundedLength}");
        //Debug.Assert(BitOperations.IsPow2(boundedLength), $"Must be a power of 2, got {boundedLength}");

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

        // Loop in case of contention...
        SpinWait spinner = default;
        while (true)
        {
            // Get the head at which to try to dequeue.
            int currentHead = Volatile.Read(ref _headAndTail.Head);
            int slotsIndex = currentHead & _slotsMask;

            // Read the sequence number for the head position.
            int sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);

            // We can dequeue from this slot if it's been filled by an enqueuer, which
            // would have left the sequence number at pos+1.
            int diff = sequenceNumber - (currentHead + 1);
            if (diff == 0)
            {
                if (Interlocked.CompareExchange(ref _headAndTail.Head, currentHead + 1, currentHead) == currentHead)
                {
                    // Successfully reserved the slot.  Note that after the above CompareExchange, other threads
                    // trying to dequeue from this slot will end up spinning until we do the subsequent Write.
                    item = slots[slotsIndex].Item!;
                    if (!Volatile.Read(ref _preservedForObservation))
                    {
                        // If we're preserving, though, we don't zero out the slot, as we need it for
                        // enumerations, peeking, ToArray, etc.  And we don't update the sequence number,
                        // so that an enqueuer will see it as full and be forced to move to a new segment.
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
            // In order to ensure we don't get a torn read on the value, we mark the segment
            // as preserving for observation.  Additional items can still be enqueued to this
            // segment, but no space will be freed during dequeues, such that the segment will
            // no longer be reusable.
            _preservedForObservation = true;
            Interlocked.MemoryBarrier();
        }

        Slot[] slots = _slots;

        // Loop in case of contention...
        SpinWait spinner = default;
        while (true)
        {
            // Get the head at which to try to peek.
            int currentHead = Volatile.Read(ref _headAndTail.Head);
            int slotsIndex = currentHead & _slotsMask;

            // Read the sequence number for the head position.
            int sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);

            // We can peek from this slot if it's been filled by an enqueuer, which
            // would have left the sequence number at pos+1.
            int diff = sequenceNumber - (currentHead + 1);
            if (diff == 0)
            {
                result = resultUsed ? slots[slotsIndex].Item! : default!;
                return true;
            }
            else if (diff < 0)
            {
                // The sequence number was less than what we needed, which means this slot doesn't
                // yet contain a value we can peek, i.e. the segment is empty.  Technically it's
                // possible that multiple enqueuers could have written concurrently, with those
                // getting later slots actually finishing first, so there could be elements after
                // this one that are available, but we need to peek in order.  So before declaring
                // failure and that the segment is empty, we check the tail to see if we're actually
                // empty or if we're just waiting for items in flight or after this one to become available.
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

        // Loop in case of contention...
        while (true)
        {
            // Get the tail at which to try to return.
            int currentTail = Volatile.Read(ref _headAndTail.Tail);
            int slotsIndex = currentTail & _slotsMask;

            // Read the sequence number for the tail position.
            int sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);

            // The slot is empty and ready for us to enqueue into it if its sequence
            // number matches the slot.
            int diff = sequenceNumber - currentTail;
            if (diff == 0)
            {
                if (Interlocked.CompareExchange(ref _headAndTail.Tail, currentTail + 1, currentTail) == currentTail)
                {
                    // Successfully reserved the slot.  Note that after the above CompareExchange, other threads
                    // trying to return will end up spinning until we do the subsequent Write.
                    slots[slotsIndex].Item = item;
                    Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentTail + 1);
                    return true;
                }

                // The tail was already advanced by another thread. A newer tail has already been observed and the next
                // iteration would make forward progress, so there's no need to spin-wait before trying again.
            }
            else if (diff < 0)
            {
                // The sequence number was less than what we needed, which means this slot still
                // contains a value, i.e. the segment is full.  Technically it's possible that multiple
                // dequeuers could have read concurrently, with those getting later slots actually
                // finishing first, so there could be spaces after this one that are available, but
                // we need to enqueue in order.
                return false;
            }
            
        }
    }

    /// <summary>Represents a slot in the queue.</summary>
    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("Item = {Item}, SequenceNumber = {SequenceNumber}")]
    internal struct Slot
    {
        /// <summary>The item.</summary>
        public T? Item; // SOS's ThreadPool command depends on this being at the beginning of the struct when T is a reference type
        /// <summary>The sequence number for this slot, used to synchronize between enqueuers and dequeuers.</summary>
        public int SequenceNumber;
    }
}

internal class PaddingHelpers
{
    public const int CACHE_LINE_SIZE = 128;
}

/// <summary>Padded head and tail indices, to avoid false sharing between producers and consumers.</summary>
[DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
[StructLayout(LayoutKind.Explicit, Size = 3 * PaddingHelpers.CACHE_LINE_SIZE)] // padding before/between/after fields
internal struct PaddedHeadAndTail
{
    [FieldOffset(1 * PaddingHelpers.CACHE_LINE_SIZE)] public int Head;
    [FieldOffset(2 * PaddingHelpers.CACHE_LINE_SIZE)] public int Tail;
}