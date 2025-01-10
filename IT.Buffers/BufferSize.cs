namespace IT.Buffers;

public static class BufferSize
{
    public const int Min = 256;
    public const int KB_4 = 4096;
    public const int KB_256 = 262144;
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