using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

internal class HybridArrayPool<T> : ArrayPool<T>
{
    private const int LastBucketIndex = 27;

    private readonly int _sharedMaxLength;
    private readonly int _sharedMaxIndex;
    private readonly ArrayBucket<T>[] _buckets;
    private readonly ArrayBucket<T>? _lastBucket;

    public HybridArrayPool(HybridArrayPoolOptions options)
    {
        var pow2s = options.Pow2s;
        var length = pow2s.Length;
        if (length == HybridArrayPoolOptions.MaxLength)
        {
            length--;
            _lastBucket = new ArrayBucket<T>(LastBucketIndex, pow2s[LastBucketIndex]);
        }
        var sharedMaxIndex = options.GetSharedMaxIndex();
        if (sharedMaxIndex > -1)
        {
            _sharedMaxLength = xArray.GetMaxSizeForBucket(sharedMaxIndex);
            sharedMaxIndex++;
            _sharedMaxIndex = sharedMaxIndex;
            length -= sharedMaxIndex;
        }
        if (length > 0)
        {
            var buckets = new ArrayBucket<T>[length];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new ArrayBucket<T>(i, pow2s[i]);
            }
            _buckets = buckets;
        }
        else
        {
            _buckets = [];
        }
    }

    public override T[] Rent(int minimumLength)
    {
        if (minimumLength <= _sharedMaxLength)
        {
            return Shared.Rent(minimumLength);
        }
        int bucketIndex = xArray.SelectBucketIndex(minimumLength) - _sharedMaxIndex;
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
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        var length = array.Length;
        if (length <= _sharedMaxLength)
        {
            Shared.Return(array, clearArray);
        }
        else
        {
            int bucketIndex = xArray.SelectBucketIndex(length) - _sharedMaxIndex;
            var buckets = _buckets;
            if ((uint)bucketIndex < (uint)buckets.Length)
            {
                buckets[bucketIndex].TryEnqueue(array, clearArray);
            }
            else if (bucketIndex == LastBucketIndex)
            {
                var lastBucket = _lastBucket;
                if (lastBucket != null)
                {
                    lastBucket.TryEnqueue(array, clearArray);
                }
            }
        }
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