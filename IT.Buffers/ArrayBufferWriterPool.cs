using IT.Buffers.Interfaces;
using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace IT.Buffers;

public class ArrayBufferWriterPool<T> : IBufferPool<ArrayBufferWriter<T>>
{
    public static readonly ArrayBufferWriterPool<T> Shared = new();

    private readonly ConcurrentQueue<ArrayBufferWriter<T>> _queue = new();

    public ArrayBufferWriter<T> Rent() => _queue.TryDequeue(out var buffer) ? buffer : new ArrayBufferWriter<T>();

    public bool Return(ArrayBufferWriter<T> buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

#if NET8_0_OR_GREATER
        buffer.ResetWrittenCount();
#else
        buffer.Clear();
#endif
        _queue.Enqueue(buffer);

        return true;
    }
}