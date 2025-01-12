namespace IT.Buffers;

public static class BufferSize
{
    public const int Min = 256;//2^8
    public const int KB = 1024;//2^10
    public const int KB_2 = 2048;//2^11
    public const int KB_4 = 4096;//2^12
    public const int KB_8 = 8192;//2^13
    public const int KB_16 = 16384;//2^14
    public const int KB_32 = 32768;//2^15
    public const int KB_64 = 65536;//2^16
    public const int KB_83 = 84992;
    public const int LOH = 85000;
    public const int KB_128 = 131072;//2^17
    public const int KB_256 = 262144;//2^18
    public const int KB_512 = 524288;//2^19
    public const int MB = 1048576;//2^20
    public const int Max = 0X7FFFFFC7;
    public const int MaxHalf = Max / 2;

#if NET
    static BufferSize()
    {
        System.Diagnostics.Debug.Assert(Max == System.Array.MaxLength);
    }
#endif

    public static int GetDoubleCapacity(int size)
    {
        var newSize = unchecked(size * 2);
        if ((uint)newSize > Max)
        {
            newSize = Max;
        }
        return newSize;
    }
}