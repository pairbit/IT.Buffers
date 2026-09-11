using System;

namespace IT.Buffers;

public interface IBufferPool
{
    int Id { get; }

    Type BufferType { get; }

    //bool IsBounded { get; }
}