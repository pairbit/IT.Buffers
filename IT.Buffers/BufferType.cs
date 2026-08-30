namespace IT.Buffers;

public enum BufferType : sbyte
{
    //ShortBlob = -5
    //Stream = -4
    //String = -3
    //Sequence = -2
    Unknown = -1,
    Null = 0,
    Array = 1,
    MemoryManager = 2,
    MemoryOwner = 3,
}