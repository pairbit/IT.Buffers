using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

internal class BoundedArrayPool<T> : ArrayPool<T>
{
    private const int NumBuckets = 27;
    private readonly BoundedConcurrentQueue<T[]>?[] _buckets = new BoundedConcurrentQueue<T[]>[NumBuckets];

    public BoundedArrayPool()
    {
        //_queue = new(power2);
    }

    public override T[] Rent(int minimumLength)
    {
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
        else if (minimumLength == 0)
        {
            return [];
        }
        else if (minimumLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumLength));
        }

        return xArray.AllocateUninitialized<T>(minimumLength);
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        int bucketIndex = xArray.SelectBucketIndex(array.Length);

        throw new NotImplementedException();
    }
}