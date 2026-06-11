using System;
using System.Linq;

namespace IT.Buffers;

public readonly struct BoundedArrayPoolOptions
{
    internal const int MaxLength = 28;
    private readonly byte[] _pow2s;

    public ReadOnlySpan<byte> Pow2s => _pow2s;

    public BoundedArrayPoolOptions()
    {
        _pow2s = new byte[MaxLength];
    }

    public BoundedArrayPoolOptions(int length)
    {
        if (length <= 0 || length > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(length));

        _pow2s = new byte[length];
    }

    public BoundedArrayPoolOptions(byte[] pow2s)
    {
        var length = pow2s.Length;
        if (length == 0 || length > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(pow2s));

        for (int i = 0; i < pow2s.Length; i++)
        {
            var pow2 = pow2s[i];
            if (pow2 > 30)
                throw new ArgumentOutOfRangeException(nameof(pow2s));
        }
        _pow2s = pow2s;
    }

    public BoundedArrayPoolOptions SetPow2(byte pow2)
    {
        if (pow2 < 1 || pow2 > 30)
            throw new ArgumentOutOfRangeException(nameof(pow2));

        var pow2s = _pow2s.ToArray();

        for (int i = 0; i < pow2s.Length; i++)
        {
            pow2s[i] = pow2;
        }

        return new(pow2s);
    }
}