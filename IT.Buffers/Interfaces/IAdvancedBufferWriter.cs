using System;
using System.Buffers;

namespace IT.Buffers.Interfaces;

public interface IAdvancedBufferWriter<T> : IBufferWriter<T>
{
    int Written { get; }

    void ResetWritten();

    bool TryWrite(Span<T> span);

    void Write<TBufferWriter>(ref TBufferWriter writer) where TBufferWriter : IBufferWriter<T>;
}

public interface ILongAdvancedBufferWriter<T> : IAdvancedBufferWriter<T>
{
    long WrittenLong { get; }

    int WrittenSegments { get; }

    Memory<T> GetWrittenMemory(int segment = 0);
}

/*
public interface ISimpleBufferWriter<T> : IAdvancedBufferWriter<T>
{
    ReadOnlyMemory<T> WrittenMemory { get; }

    ReadOnlySpan<T> WrittenSpan { get; }

    int Capacity { get; }

    int FreeCapacity { get; }
}

internal interface IByteAdvancedBufferWriter : IAdvancedBufferWriter<byte>
{
    void Write(Stream stream);

    ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken);
}
*/