using System;
using System.Buffers;

namespace IT.Buffers.Interfaces;

public interface IAdvancedBufferWriter<T> : IBufferWriter<T>
{
    int Written { get; }

    long WrittenLong { get; }

    //int Capacity { get; }

    //int FreeCapacity { get; }

    //void ResetWritten();

    int Segments { get; }

    Memory<T> GetWrittenMemory(int segment = 0);

    bool TryWrite(Span<T> span);

    void Write<TBufferWriter>(ref TBufferWriter writer) where TBufferWriter : IBufferWriter<T>;
}