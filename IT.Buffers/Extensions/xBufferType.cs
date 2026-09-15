namespace IT.Buffers.Extensions;

public static class xBufferType
{
    public static bool IsMemory(this BufferType type)
    {
        var no = (byte)type;
        return no >= 0 || no <= 3;
    }

    public static bool IsSequence(this BufferType type) =>
        type == BufferType.Sequence || type == BufferType.SequenceOwner;
}