using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace IT.Buffers;

//14/10/2022 6:41 AM
//https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Concurrent/ConcurrentQueueSegment.cs
[DebuggerDisplay("Capacity = {Capacity}")]
public sealed class BoundedConcurrentQueue<T>
{
    private readonly Slot[] _slots; // SOS's ThreadPool command depends on this name
    private readonly int _slotsMask;
    private PaddedHeadAndTail _headAndTail;
    private bool _preservedForObservation;
    private bool _frozenForEnqueues;

    /// <param name="boundedLength">
    /// The maximum number of elements the segment can contain.  Must be a power of 2.
    /// </param>
    public BoundedConcurrentQueue(int power2 = 5)
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
    public int Capacity => _slots.Length;

    /// <summary>Gets the "freeze offset" for this segment.</summary>
    private int FreezeOffset => _slots.Length * 2;

    public bool IsFrozen => _frozenForEnqueues;

    public void Freeze() // must only be called while queue's segment lock is held
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

    public bool IsEmpty()
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
                return false;
            }
            else if (diff < 0)
            {
                bool frozen = _frozenForEnqueues;
                int currentTail = Volatile.Read(ref _headAndTail.Tail);
                if (currentTail - currentHead <= 0 || (frozen && (currentTail - FreezeOffset - currentHead <= 0)))
                {
                    return true;
                }

                spinner.SpinOnce(
#if NET
                    sleep1Threshold: -1
#endif
                    );
            }
        }
    }

    public int GetCount()
    {
        int head = Volatile.Read(ref _headAndTail.Head);
        int tail = Volatile.Read(ref _headAndTail.Tail);
        if (head != tail && head != tail - FreezeOffset)
        {
            head &= _slotsMask;
            tail &= _slotsMask;
            return head < tail ? tail - head : _slots.Length - head + tail;
        }
        return 0;
    }

    /// <summary>Tries to peek at an element from the queue, without removing it.</summary>
    public bool TryPeek([MaybeNullWhen(false)] out T item)
    {
        _preservedForObservation = true;
        Interlocked.MemoryBarrier();

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
                item = slots[slotsIndex].Item!;
                return true;
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

[DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
[StructLayout(LayoutKind.Explicit, Size = 3 * PaddingHelpers.CACHE_LINE_SIZE)]
internal struct PaddedHeadAndTail
{
    [FieldOffset(1 * PaddingHelpers.CACHE_LINE_SIZE)] public int Head;
    [FieldOffset(2 * PaddingHelpers.CACHE_LINE_SIZE)] public int Tail;
}

//https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/Internal/Padding.cs
internal static class PaddingHelpers
{
#if TARGET_ARM64 || TARGET_LOONGARCH64
    internal const int CACHE_LINE_SIZE = 128;
#else
    internal const int CACHE_LINE_SIZE = 64;
#endif
}