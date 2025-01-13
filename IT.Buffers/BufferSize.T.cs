using System.Runtime.CompilerServices;

namespace IT.Buffers;

public static class BufferSize<T>
{
    public static readonly int Min;
    public static readonly int KB;
    public static readonly int KB_2;
    public static readonly int KB_4;
    public static readonly int KB_8;
    public static readonly int KB_16;
    public static readonly int KB_32;
    public static readonly int KB_64;
    public static readonly int KB_80;
    public static readonly int KB_83;
    public static readonly int LOH;
    public static readonly int KB_128;
    public static readonly int KB_256;
    public static readonly int KB_512;
    public static readonly int MB;
    public static readonly int Max;
    public static readonly int MaxHalf;

    static BufferSize()
    {
        var sizeType = Unsafe.SizeOf<T>();
        Min = BufferSize.Min / sizeType;
        KB = BufferSize.KB / sizeType;
        KB_2 = BufferSize.KB_2 / sizeType;
        KB_4 = BufferSize.KB_4 / sizeType;
        KB_16 = BufferSize.KB_16 / sizeType;
        KB_32 = BufferSize.KB_32 / sizeType;
        KB_64 = BufferSize.KB_64 / sizeType;
        KB_80 = BufferSize.KB_80 / sizeType;
        KB_83 = BufferSize.KB_83 / sizeType;
        LOH = BufferSize.LOH / sizeType;
        KB_128 = BufferSize.KB_128 / sizeType;
        KB_256 = BufferSize.KB_256 / sizeType;
        KB_512 = BufferSize.KB_512 / sizeType;
        MB = BufferSize.MB / sizeType;
        Max = BufferSize.Max / sizeType;
        MaxHalf = BufferSize.MaxHalf / sizeType;
    }
}