using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers;

public interface IAdvancedBufferWriter<T> : IBufferWriter<T>
{
    int Written { get; }

    long WrittenLong { get; }

    //bool TryWrite(Span<T> span);

    //void Write<TBufferWriter>(in TBufferWriter writer) where TBufferWriter : IBufferWriter<T>;
}

//public interface ISimpleBufferWriter<T> : IAdvancedBufferWriter<T>
//{
//    ReadOnlyMemory<T> WrittenMemory { get; }

//    ReadOnlySpan<T> WrittenSpan { get; }
     
//    int Capacity { get; }

//    int FreeCapacity { get; }
//}

//internal interface IByteAdvancedBufferWriter : IAdvancedBufferWriter<byte>
//{
//    void Write(Stream stream);

//    ValueTask WriteAsync(Stream stream, CancellationToken cancellationToken);
//}