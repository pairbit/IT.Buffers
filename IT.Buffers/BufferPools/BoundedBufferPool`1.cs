using System;
using System.Diagnostics.CodeAnalysis;

namespace IT.Buffers;

public abstract class BoundedBufferPool<TBuffer> : IBufferPool<TBuffer> where TBuffer : IResetable
{
    private readonly BoundedConcurrentQueue<TBuffer> _queue;
    private readonly int _id;

    public int Id => _id;

    public Type BufferType => typeof(TBuffer);

    public BoundedBufferPool(int id, int pow2 = 5)
    {
        if (id != 0) throw new ArgumentOutOfRangeException(nameof(id));

        _queue = new(pow2);
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

        buffer.Reset();

        return _queue.TryEnqueue(buffer);
    }

    public void Return(TBuffer buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

        buffer.Reset();

        if (!_queue.TryEnqueue(buffer))
            throw new InvalidOperationException("The buffer pool is full.");
    }

    protected abstract TBuffer NewBuffer();
}