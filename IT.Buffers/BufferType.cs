namespace IT.Buffers;

public enum BufferType : sbyte
{
    Unknown = -128,
    //Stream = -3
    //String = -2
    //Span64 = -1
    Null = 0,
    Array = 1,
    MemoryManager = 2,
    MemoryOwner = 3,
    Sequence = 4,
    SequenceOwner = 5
}