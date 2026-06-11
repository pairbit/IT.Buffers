using System;
using System.Buffers;
using System.Linq;

namespace IT.Buffers;

internal readonly struct HybridArrayPoolOptions
{
    internal const int MaxLength = 28;
    private const byte Pow2_Shared = 31;

    private readonly byte[] _pow2s;

    public ReadOnlySpan<byte> Pow2s => _pow2s;

    public HybridArrayPoolOptions()
    {
        _pow2s = new byte[MaxLength];
    }

    public HybridArrayPoolOptions(int length)
    {
        if (length <= 0 || length > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(length));

        _pow2s = new byte[length];
    }

    public HybridArrayPoolOptions(byte[] pow2s)
    {
        var length = pow2s.Length;
        if (length == 0 || length > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(pow2s));

        for (int i = 0; i < pow2s.Length; i++)
        {
            var pow2 = pow2s[i];
            if (pow2 > Pow2_Shared)
                throw new ArgumentOutOfRangeException(nameof(pow2s));
        }
        _pow2s = pow2s;
    }

    public int GetSharedMaxIndex()
    {
        return _pow2s.LastIndexOf(Pow2_Shared);
    }

    public HybridArrayPoolOptions SetPow2(byte pow2)
    {
        if (pow2 < 1 || pow2 > Pow2_Shared)
            throw new ArgumentOutOfRangeException(nameof(pow2));

        var pow2s = _pow2s.ToArray();

        for (int i = 0; i < pow2s.Length; i++)
        {
            pow2s[i] = pow2;
        }

        return new(pow2s);
    }

    public static HybridArrayPoolOptions Create() => new([
        Pow2_Shared,//16
        Pow2_Shared,//32
        Pow2_Shared,//64
        Pow2_Shared,//128
        Pow2_Shared,//256
        Pow2_Shared,//512
        Pow2_Shared,//1KB
        Pow2_Shared,//2KB
        Pow2_Shared,//4KB
        Pow2_Shared,//8KB
        Pow2_Shared,//16KB
        Pow2_Shared,//32KB
        Pow2_Shared,//64KB
        Pow2_Shared,//128KB
        Pow2_Shared,//256KB
        Pow2_Shared,//512KB
        Pow2_Shared,//1MB
        3,//2MB
        3,//4MB
        3,//8MB
        3,//16MB
        2,//32MB
        1,//64MB
        0,//128MB
        0,//256MB
        0,//512MB
        0,//1GB
        0//MAX
    ]);
}