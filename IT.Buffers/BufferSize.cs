namespace IT.Buffers;

public static class BufferSize
{
    public const int Min = 256;
    public const int Max = 0X7FFFFFC7;
    public const int MaxHalf = Max / 2;

#if NET
    static BufferSize()
    {
        System.Diagnostics.Debug.Assert(Max == System.Array.MaxLength);
    }
#endif
}