using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace IT.Buffers;

public abstract class BufferPool<TBuffer> : IBufferPool<TBuffer>
{
    private readonly ConcurrentQueue<TBuffer> _queue;
    private readonly int _id;

    public int Id => _id;

    public Type BufferType => typeof(TBuffer);

    protected BufferPool()
    {
        _queue = new();
    }

    public BufferPool(int id)
    {
        if (id != 0) throw new ArgumentOutOfRangeException(nameof(id));

        _queue = new();
        _id = id;
    }

    public BufferPool(IEnumerable<TBuffer> buffers, int id)
    {
        if (id != 0) throw new ArgumentOutOfRangeException(nameof(id));

        _queue = new(buffers);
        _id = id;
    }

    public bool TryRent([MaybeNullWhen(false)] out TBuffer buffer) =>
        _queue.TryDequeue(out buffer);

    public TBuffer Rent()
    {
        if (!_queue.TryDequeue(out var buffer))
        {
            buffer = NewBuffer();
        }

        return buffer;
    }

    /// <exception cref="ArgumentNullException"></exception>
    public bool TryReturn(TBuffer buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

        _queue.Enqueue(buffer);

        return true;
    }

    public void Return(TBuffer buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

        _queue.Enqueue(buffer);
    }

    protected abstract TBuffer NewBuffer();
}