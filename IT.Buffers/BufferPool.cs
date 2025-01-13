using IT.Buffers.Interfaces;
using System;
using System.Collections.Concurrent;

namespace IT.Buffers;

public class BufferPool<TBuffer> : IBufferPool<TBuffer> where TBuffer : class, IDisposable, new()
{
    private readonly ConcurrentQueue<TBuffer> _queue = new();

    public TBuffer Rent() => _queue.TryDequeue(out var buffer) ? buffer : new TBuffer();

    /// <exception cref="ArgumentNullException"></exception>
    public void Return(TBuffer buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

        buffer.Dispose();

        _queue.Enqueue(buffer);
    }
}