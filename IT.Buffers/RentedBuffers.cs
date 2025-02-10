using System;
using System.Buffers;
using System.Collections.Generic;

namespace IT.Buffers;

public class RentedBuffers
{
    private readonly List<RentedBuffer> _list;
    private int _depth;

    public RentedBuffers()
    {
        _list = [];
    }

    public RentedBuffers(int capacity)
    {
        _list = new List<RentedBuffer>(capacity);
    }

    public int Track()
    {
        return ++_depth;
    }

    public bool AddArray<T>(T[] array)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length == 0) throw new ArgumentException("is empty", nameof(array));

        var depth = _depth;
        if (depth == 0) return false;

        _list.Add(new RentedBuffer(depth, array, ReturnArray<T>));
        return true;
    }

    public bool AddSequence<T>(in ReadOnlySequence<T> sequence)
    {
        if (sequence.Start.GetObject() is not SequenceSegment<T> segment)
            throw new ArgumentException("does not contain SequenceSegment", nameof(sequence));

        if (sequence.Length == 0) throw new ArgumentException("is empty", nameof(sequence));

        var depth = _depth;
        if (depth == 0) return false;

        _list.Add(new RentedBuffer(depth, segment, ReturnSegments<T>));
        return true;
    }

    public int ReturnAndClear()
    {
        var depth = _depth;
        if (depth == 0) return -1;

        var list = _list;
        if (list.Count > 0)
        {
            var returned = 0;
#if NET6_0_OR_GREATER
            var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);

            for (var i = span.Length - 1; i >= 0; i--)
            {
                var rentedBuffer = span[i];

                if (rentedBuffer.Depth != depth) break;

                rentedBuffer.Return(rentedBuffer.Buffer);

                returned++;
            }
            if (returned > 0) span[..returned].Clear();
#else
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var rentedBuffer = list[i];

                if (rentedBuffer.Depth != depth) break;

                rentedBuffer.Return(rentedBuffer.Buffer);

                list[i] = default;

                returned++;
            }
#endif
            return returned;
        }
        _depth--;
        return 0;
    }

    public int Clear()
    {
        var depth = _depth;
        if (depth == 0) return -1;

        var list = _list;
        if (list.Count > 0)
        {
            var clear = 0;
#if NET6_0_OR_GREATER
            var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
            
            for (var i = span.Length - 1; i >= 0; i--)
            {
                var rentedBuffer = span[i];
                if (rentedBuffer.Depth != depth) break;
                clear++;
            }
            if (clear > 0) span[..clear].Clear();
#else
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var rentedBuffer = list[i];
                if (rentedBuffer.Depth != depth) break;

                clear++;

                list[i] = default;
            }
#endif
            return clear;
        }
        _depth--;
        return 0;
    }

    private static void ReturnArray<T>(object array)
        => BufferPool.Return((T[])array);

    private static void ReturnSegments<T>(object segment)
        => BufferPool.TryReturnSegments((SequenceSegment<T>)segment);

    delegate void ReturnBuffer(object buffer);

    readonly struct RentedBuffer
    {
        public readonly int Depth;
        public readonly object Buffer;
        public readonly ReturnBuffer Return;

        public RentedBuffer(int depth, object buffer, ReturnBuffer returnBuffer)
        {
            Depth = depth;
            Buffer = buffer;
            Return = returnBuffer;
        }
    }
}