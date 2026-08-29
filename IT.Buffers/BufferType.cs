namespace IT.Buffers;

public enum BufferType : byte
{
    Null = 0,
    Array,
    MemoryManager,
    MemoryOwner,
    //Sequence,
    //String,
    //ShortBlob,
    //Stream
    Unknown
}