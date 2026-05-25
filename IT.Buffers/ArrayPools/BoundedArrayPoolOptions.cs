using System;

namespace IT.Buffers;

internal readonly struct BoundedArrayPoolOptions
{
    internal const int BucketsCount = 27;
    private readonly sbyte[] _pow2s;

    public ReadOnlySpan<sbyte> Pow2s => _pow2s;

    public BoundedArrayPoolOptions()
    {
        _pow2s = new sbyte[BucketsCount];
    }

    public BoundedArrayPoolOptions(sbyte[] pow2s)
    {
        if (pow2s.Length != BucketsCount)
            throw new ArgumentOutOfRangeException(nameof(pow2s));

        _pow2s = pow2s;
    }
}