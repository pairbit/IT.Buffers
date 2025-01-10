using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers;

internal interface IAdvancedBufferWriter<T> : IBufferWriter<T>
{
    int Written { get; }

    long WrittenLong { get; }

    bool TryWrite(Span<T> span);

    void Write<TBufferWriter>(in TBufferWriter writer) where TBufferWriter : IBufferWriter<T>;
}

internal interface IByteAdvancedBufferWriter : IAdvancedBufferWriter<byte>
{
    void Write(Stream stream);

    ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken);
}