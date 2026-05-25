using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;

namespace IT.Buffers;

internal class BoundedArrayPool<T> : ArrayPool<T>
{
    private readonly BoundedArrayPoolOptions _options;
    private readonly BoundedConcurrentQueue<T[]>?[] _buckets = new BoundedConcurrentQueue<T[]>[BoundedArrayPoolOptions.Length];

    public BoundedArrayPool(BoundedArrayPoolOptions options)
    {
        _options = options;
    }

    public override T[] Rent(int minimumLength)
    {
        if (minimumLength < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumLength));

        if (minimumLength == 0)
            return [];

        int bucketIndex = xArray.SelectBucketIndex(minimumLength);
        var buckets = _buckets;
        if ((uint)bucketIndex < (uint)buckets.Length)
        {
            var bucket = buckets[bucketIndex];
            if (bucket is not null)
            {
                if (bucket.TryDequeue(out var buffer))
                {
                    Debug.Assert(buffer.Length >= minimumLength);
                    return buffer;
                }
            }

            // No buffer available.  Ensure the length we'll allocate matches that of a bucket
            // so we can later return it.
            minimumLength = xArray.GetMaxSizeForBucket(bucketIndex);
        }

        return xArray.AllocateUninitialized<T>(minimumLength);
    }

    public override void Return(T[] array, bool clearArray = false)
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
            {
                throw new ArgumentException("Buffer not from pool.", nameof(array));
            }

            var bucket = _buckets[bucketIndex] ?? CreateBucket(bucketIndex);
            var added = bucket.TryEnqueue(array);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = null;
        }
    }

    private BoundedConcurrentQueue<T[]> CreateBucket(int bucketIndex)
    {
        var bucket = new BoundedConcurrentQueue<T[]>(_options.Pow2s[bucketIndex]);
        return Interlocked.CompareExchange(ref _buckets[bucketIndex], bucket, null) ?? bucket;
    }
}