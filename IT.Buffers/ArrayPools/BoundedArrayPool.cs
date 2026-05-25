using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;

namespace IT.Buffers;

internal class BoundedArrayPool<T> : ArrayPool<T>
{
    private readonly object _null = new();
    private readonly object _empty = new();
    private readonly BoundedArrayPoolOptions _options;
    private readonly object?[] _buckets;

    public BoundedArrayPool(BoundedArrayPoolOptions options)
    {
        _options = options;

        var buckets = new object?[BoundedArrayPoolOptions.Length];
        var pow2s = options.Pow2s;
        Debug.Assert(pow2s.Length == buckets.Length);

        for (int i = 0; i < buckets.Length; i++)
        {
            var pow2 = pow2s[i];
            if (pow2 < 0)
            {
                buckets[i] = _null;
            }
            else if (pow2 == 0)
            {
                buckets[i] = _empty;
            }
        }
        _buckets = buckets;
    }

    public override T[] Rent(int minimumLength)
    {
        if (minimumLength < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumLength));

        int bucketIndex = xArray.SelectBucketIndex(minimumLength);
        var buckets = _buckets;
        if ((uint)bucketIndex < (uint)buckets.Length)
        {
            var bucket = buckets[bucketIndex];
            if (bucket is BoundedConcurrentQueue<T[]> queue)
            {
                if (queue.TryDequeue(out var buffer))
                {
                    Debug.Assert(buffer.Length >= minimumLength);
                    return buffer;
                }
            }
            else if (bucket is T[] buffer)
            {
                Debug.Assert(buffer.Length >= minimumLength);
                if (Interlocked.CompareExchange(ref buckets[bucketIndex], null, buffer) == buffer)
                {
                    return buffer;
                }
            }

            // No buffer available.  Ensure the length we'll allocate matches that of a bucket
            // so we can later return it.
            minimumLength = xArray.GetMaxSizeForBucket(bucketIndex);
        }
        else if (minimumLength == 0)
        {
            return [];
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

        if (clearArray)
        {
            array.Clear();
        }

        int bucketIndex = xArray.SelectBucketIndex(array.Length);
        var buckets = _buckets;
        if ((uint)bucketIndex < (uint)buckets.Length)
        {
            if (array.Length != xArray.GetMaxSizeForBucket(bucketIndex))
                throw new ArgumentException("Buffer not from pool.", nameof(array));

            var bucket = buckets[bucketIndex] ?? CreateQueue(bucketIndex);
            if (bucket is BoundedConcurrentQueue<T[]> queue)
            {
                return queue.TryEnqueue(array);
            }
            else if (bucket == _empty)
            {
                return Interlocked.CompareExchange(ref buckets[bucketIndex], array, _empty) == _empty;
            }
        }

        return false;
    }

    public void Clear()
    {
        var buckets = _buckets;
        for (int i = 0; i < buckets.Length; i++)
        {
            var bucket = buckets[i];
            if (bucket is BoundedConcurrentQueue<T[]>)
            {
                buckets[i] = null;
            }
            else if (bucket is T[])
            {
                buckets[i] = _empty;
            }
        }
    }

    private object CreateQueue(int bucketIndex)
    {
        var bucket = new BoundedConcurrentQueue<T[]>(_options.Pow2s[bucketIndex]);
        return Interlocked.CompareExchange(ref _buckets[bucketIndex], bucket, null) ?? bucket;
    }
}