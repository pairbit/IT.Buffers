using IT.Buffers.Internal;
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
    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("Item = {Item}, SequenceNumber = {SequenceNumber}")]
    struct Slot
    {
        public T? Item;
        public int SequenceNumber;
    }

    private readonly Slot[] _slots;
    private readonly int _slotsMask;
    private PaddedHeadAndTail _headAndTail;

    /// <param name="boundedLength">
    /// The maximum number of elements the segment can contain.  Must be a power of 2.
    /// </param>
    public BoundedConcurrentQueue(int pow2 = 5)
    {
        if (pow2 < 1 || pow2 > 30)
            throw new System.ArgumentOutOfRangeException(nameof(pow2));

        var boundedLength = 2 << (pow2 - 1);

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

                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        slots[slotsIndex].Item = default;
                    }
                    Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentHead + slots.Length);

                    return true;
                }
            }
            else if (diff < 0)
            {
                int currentTail = Volatile.Read(ref _headAndTail.Tail);
                if (currentTail - currentHead <= 0)
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
                int currentTail = Volatile.Read(ref _headAndTail.Tail);
                if (currentTail - currentHead <= 0)
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
        if (head != tail)
        {
            head &= _slotsMask;
            tail &= _slotsMask;
            return head < tail ? tail - head : _slots.Length - head + tail;
        }
        return 0;
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
}