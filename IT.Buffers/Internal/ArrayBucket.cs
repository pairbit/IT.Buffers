using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace IT.Buffers.Internal;

internal class ArrayBucket<T>
{
    private static readonly object _empty = new();

    public readonly int _length;
    public readonly int _pow2;
    public object? _obj;

    public BoundedConcurrentQueue<T[]>? Queue => _obj as BoundedConcurrentQueue<T[]>;

    public T[]? Array => _obj as T[];

    public ArrayBucket(int index, int pow2)
    {
        _length = xArray.GetMaxSizeForBucket(index);
        _pow2 = pow2;

        if (pow2 == 0)
            _obj = _empty;
    }

    public bool TryEnqueue(T[] array, bool clearArray = false)
    {
        Debug.Assert(array != null);

        if (array.Length != _length)
            throw new ArgumentException("Buffer not from pool.", nameof(array));

        if (clearArray)
            array.Clear();

        var obj = _obj ?? CreateQueue();
        if (obj is BoundedConcurrentQueue<T[]> queue)
        {
            return queue.TryEnqueue(array);
        }

        if (ReferenceEquals(obj, _empty))
            return Interlocked.CompareExchange(ref _obj, array, _empty) == _empty;

        return false;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T[] array)
    {
        var obj = _obj;
        if (obj is BoundedConcurrentQueue<T[]> queue)
        {
            if (queue.TryDequeue(out array))
            {
                Debug.Assert(array.Length == _length);

                return true;
            }
        }
        else if (obj is T[] buffer)
        {
            do
            {
                Debug.Assert(buffer.Length == _length);

                var value = Interlocked.CompareExchange(ref _obj, _empty, buffer);
                if (ReferenceEquals(value, buffer))
                {
                    array = buffer;
                    return true;
                }

                if (value is T[] otherBuffer)
                {
                    buffer = otherBuffer;
                }
                else
                {
                    break;
                }
            } while (true);
        }

        array = default;
        return false;
    }

    public void Clear()
    {
        var obj = _obj;
        if (obj is BoundedConcurrentQueue<T[]>)
        {
            _obj = null;
        }
        else if (obj is T[] array)
        {
            Debug.Assert(array.Length == _length);

            _obj = _empty;
        }
    }

    private object CreateQueue()
    {
        var queue = new BoundedConcurrentQueue<T[]>(_pow2);
        return Interlocked.CompareExchange(ref _obj, queue, null) ?? queue;
    }
}