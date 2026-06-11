using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace IT.Buffers;

public class BoundedArrayPool<T> : ArrayPool<T>
{
    private const int LastBucketIndex = 27;
    private static readonly object _empty = new();

    private readonly Bucket[] _buckets;
    private readonly Bucket? _lastBucket;

    public BoundedArrayPool(BoundedArrayPoolOptions options)
    {
        var pow2s = options.Pow2s;
        var length = pow2s.Length;
        if (length == BoundedArrayPoolOptions.MaxLength)
        {
            length--;
            _lastBucket = new Bucket(LastBucketIndex, pow2s[LastBucketIndex]);
        }
        var buckets = new Bucket[length];
        for (int i = 0; i < buckets.Length; i++)
        {
            buckets[i] = new Bucket(i, pow2s[i]);
        }
        _buckets = buckets;
    }

    public override T[] Rent(int minimumLength)
    {
        int bucketIndex = xArray.SelectBucketIndex(minimumLength);
        var buckets = _buckets;
        if ((uint)bucketIndex < (uint)buckets.Length)
        {
            var bucket = buckets[bucketIndex];
            if (bucket.TryDequeue(out var buffer))
            {
                Debug.Assert(buffer.Length >= minimumLength);

                return buffer;
            }

            minimumLength = bucket._length;
        }
        else if (minimumLength == 0)
        {
            return [];
        }
        else if (minimumLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumLength));
        }
        else if (bucketIndex == LastBucketIndex)
        {
            if (minimumLength > BufferSize.Max)
                throw new ArgumentOutOfRangeException(nameof(minimumLength));

            var lastBucket = _lastBucket;
            if (lastBucket != null)
            {
                if (lastBucket.TryDequeue(out var buffer))
                {
                    Debug.Assert(buffer.Length >= minimumLength);

                    return buffer;
                }

                minimumLength = lastBucket._length;
            }
        }

        return xArray.AllocateUninitialized<T>(minimumLength);
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        var added = TryReturn(array, clearArray);
    }

    public bool TryReturn(T[] array, bool clearArray = false)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        int bucketIndex = xArray.SelectBucketIndex(array.Length);
        var buckets = _buckets;
        if ((uint)bucketIndex < (uint)buckets.Length)
        {
            if (clearArray)
                array.Clear();

            return buckets[bucketIndex].TryEnqueue(array);
        }
        else if (bucketIndex == LastBucketIndex)
        {
            var lastBucket = _lastBucket;
            if (lastBucket != null)
            {
                if (clearArray)
                    array.Clear();

                return lastBucket.TryEnqueue(array);
            }
        }
        return false;
    }

    public void Clear()
    {
        var lastBucket = _lastBucket;
        if (lastBucket != null)
            lastBucket.Clear();

        var buckets = _buckets;
        for (int i = buckets.Length - 1; i >= 0; i--)
        {
            buckets[i].Clear();
        }
    }

    class Bucket
    {
        public readonly int _length;
        public readonly int _pow2;
        public object? _obj;

        public BoundedConcurrentQueue<T[]>? Queue => _obj as BoundedConcurrentQueue<T[]>;

        public T[]? Array => _obj as T[];

        public Bucket(int index, int pow2)
        {
            _length = xArray.GetMaxSizeForBucket(index);
            _pow2 = pow2;

            if (pow2 == 0)
                _obj = _empty;
        }

        public bool TryEnqueue(T[] array)
        {
            Debug.Assert(array != null);

            if (array.Length != _length)
                throw new ArgumentException("Buffer not from pool.", nameof(array));

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

                    if (value is null)
                        break;

                    buffer = (T[])value;
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
}