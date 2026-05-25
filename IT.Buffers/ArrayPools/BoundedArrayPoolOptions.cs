using System;

namespace IT.Buffers;

internal readonly struct BoundedArrayPoolOptions
{
    internal const int MaxLength = 28;
    private readonly sbyte[] _pow2s;

    public ReadOnlySpan<sbyte> Pow2s => _pow2s;

    public BoundedArrayPoolOptions()
    {
        _pow2s = new sbyte[MaxLength];
    }

    public BoundedArrayPoolOptions(int length)
    {
        if (length <= 0 || length > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(length));

        _pow2s = new sbyte[length];
    }

    public BoundedArrayPoolOptions(sbyte[] pow2s)
    {
        var length = pow2s.Length;
        if (length == 0 || length > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(pow2s));

        for (int i = 0; i < pow2s.Length; i++)
        {
            var pow2 = pow2s[i];
            if (pow2 < 0 || pow2 > 30)
                throw new ArgumentOutOfRangeException(nameof(pow2s));
        }
        _pow2s = pow2s;
    }
}