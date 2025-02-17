using System.Runtime.CompilerServices;

namespace IT.Buffers;

public static class BufferSize<T>
{
    public static readonly int Min;//2^8
    public static readonly int KB_Half;//2^9
    public static readonly int KB;//2^10
    public static readonly int KB_2;//2^11
    public static readonly int KB_4;//2^12
    public static readonly int KB_8;//2^13
    public static readonly int KB_16;//2^14
    public static readonly int KB_32;//2^15
    public static readonly int KB_64;//2^16
    public static readonly int KB_80;
    public static readonly int KB_83;
    public static readonly int LOH;
    public static readonly int KB_128;//2^17
    public static readonly int KB_256;//2^18
    public static readonly int KB_512;//2^19
    public static readonly int MB_Half;//2^19
    public static readonly int MB;//2^20
    public static readonly int MB_2;//2^21
    public static readonly int MB_4;//2^22
    public static readonly int MB_8;//2^23
    public static readonly int MB_16;//2^24
    public static readonly int MB_32;//2^25
    public static readonly int MB_64;//2^26
    public static readonly int MB_128;//2^27
    public static readonly int MB_256;//2^28
    public static readonly int MB_512;//2^29
    public static readonly int GB_Half;//2^29
    public static readonly int Max_Half;//2^30 - 29
    public static readonly int GB;//2^30
    public static readonly int Max;//2^31 - 57
#if NET
    public static readonly int Log2;
#endif

    static BufferSize()
    {
        var size = Unsafe.SizeOf<T>();
        Min = BufferSize.Min / size;
        KB_Half = BufferSize.KB_Half / size;
        KB = BufferSize.KB / size;
        KB_2 = BufferSize.KB_2 / size;
        KB_4 = BufferSize.KB_4 / size;
        KB_8 = BufferSize.KB_8 / size;
        KB_16 = BufferSize.KB_16 / size;
        KB_32 = BufferSize.KB_32 / size;
        KB_64 = BufferSize.KB_64 / size;
        KB_80 = BufferSize.KB_80 / size;
        KB_83 = BufferSize.KB_83 / size;
        LOH = BufferSize.LOH / size;
        KB_128 = BufferSize.KB_128 / size;
        KB_256 = BufferSize.KB_256 / size;
        KB_512 = MB_Half = BufferSize.KB_512 / size;
        MB = BufferSize.MB / size;
        MB_2 = BufferSize.MB_2 / size;
        MB_4 = BufferSize.MB_4 / size;
        MB_8 = BufferSize.MB_8 / size;
        MB_16 = BufferSize.MB_16 / size;
        MB_32 = BufferSize.MB_32 / size;
        MB_64 = BufferSize.MB_64 / size;
        MB_128 = BufferSize.MB_128 / size;
        MB_256 = BufferSize.MB_256 / size;
        MB_512 = GB_Half = BufferSize.MB_512 / size;
        Max_Half = BufferSize.Max_Half / size;
        GB = BufferSize.GB / size;
        Max = BufferSize.Max / size;
#if NET
        Log2 = BufferSize.Log2(size);
#endif
    }
}