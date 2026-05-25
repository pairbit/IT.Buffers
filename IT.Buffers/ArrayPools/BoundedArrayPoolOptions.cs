using System;

namespace IT.Buffers;

internal readonly struct BoundedArrayPoolOptions
{
    internal const int Length = 27;
    private readonly sbyte[] _pow2s;

    public ReadOnlySpan<sbyte> Pow2s => _pow2s;

    public BoundedArrayPoolOptions()
    {
        _pow2s = new sbyte[Length];
    }

    public BoundedArrayPoolOptions(sbyte[] pow2s)
    {
        if (pow2s.Length != Length)
            throw new ArgumentOutOfRangeException(nameof(pow2s));

        for (int i = 0; i < pow2s.Length; i++)
        {
            var pow2 = pow2s[i];
            if (pow2 < -1 || pow2 > 30)
                throw new ArgumentOutOfRangeException(nameof(pow2s));
        }
        _pow2s = pow2s;
    }
}