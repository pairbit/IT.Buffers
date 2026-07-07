using System.Diagnostics;
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
    public static readonly int Log2;

    static BufferSize()
    {
        Min = Get(BufferSize.Min);
        KB_Half = Get(BufferSize.KB_Half);
        KB = Get(BufferSize.KB);
        KB_2 = Get(BufferSize.KB_2);
        KB_4 = Get(BufferSize.KB_4);
        KB_8 = Get(BufferSize.KB_8);
        KB_16 = Get(BufferSize.KB_16);
        KB_32 = Get(BufferSize.KB_32);
        KB_64 = Get(BufferSize.KB_64);
        KB_80 = Get(BufferSize.KB_80);
        KB_83 = Get(BufferSize.KB_83);
        LOH = Get(BufferSize.LOH);
        KB_128 = Get(BufferSize.KB_128);
        KB_256 = Get(BufferSize.KB_256);
        KB_512 = MB_Half = Get(BufferSize.KB_512);
        MB = Get(BufferSize.MB);
        MB_2 = Get(BufferSize.MB_2);
        MB_4 = Get(BufferSize.MB_4);
        MB_8 = Get(BufferSize.MB_8);
        MB_16 = Get(BufferSize.MB_16);
        MB_32 = Get(BufferSize.MB_32);
        MB_64 = Get(BufferSize.MB_64);
        MB_128 = Get(BufferSize.MB_128);
        MB_256 = Get(BufferSize.MB_256);
        MB_512 = GB_Half = Get(BufferSize.MB_512);
        Max_Half = Get(BufferSize.Max_Half);
        GB = Get(BufferSize.GB);
        Max = Get(BufferSize.Max);
        Log2 = BufferSize.Log2(Unsafe.SizeOf<T>());
    }

    public static int Get(int size)
    {
        Debug.Assert(size > 0);

        return 1 + ((size - 1) / Unsafe.SizeOf<T>());
    }
}