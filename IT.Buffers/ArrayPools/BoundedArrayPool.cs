using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public class BoundedArrayPool<T> : ArrayPool<T>
{
    private const int LastBucketIndex = 27;

    private readonly ArrayBucket<T>[] _buckets;
    private readonly ArrayBucket<T>? _lastBucket;

    public BoundedArrayPool(BoundedArrayPoolOptions options)
    {
        var pow2s = options.Pow2s;
        var length = pow2s.Length;
        if (length == BoundedArrayPoolOptions.MaxLength)
        {
            length--;
            _lastBucket = new ArrayBucket<T>(LastBucketIndex, pow2s[LastBucketIndex]);
        }
        var buckets = new ArrayBucket<T>[length];
        for (int i = 0; i < buckets.Length; i++)
        {
            buckets[i] = new ArrayBucket<T>(i, pow2s[i]);
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
}