using IT.Buffers.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace IT.Buffers;

public class BufferPool<TBuffer> : IBufferPool<TBuffer> where TBuffer : class, IDisposable, new()
{
    public static readonly BufferPool<TBuffer> Shared = new();

    private readonly ConcurrentQueue<TBuffer> _queue = new();

    public TBuffer Rent()
    {
        if (!_queue.TryDequeue(out var buffer)) buffer = new TBuffer();

        if (buffer is IBufferRentable bufferRentable)
        {
            Debug.Assert(!bufferRentable.IsRented);

            bufferRentable.MakeRented();

            Debug.Assert(bufferRentable.IsRented);
        }

        return buffer;
    }

    /// <exception cref="ArgumentNullException"></exception>
    public bool TryReturn(TBuffer buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

        if (buffer is IBufferRentable bufferRentable)
        {
            //protection pool overflow. We return to the pool only rented buffers
            if (bufferRentable.IsRented)
            {
                buffer.Dispose();

                Debug.Assert(!bufferRentable.IsRented);

                _queue.Enqueue(buffer);

                return true;
            }

            buffer.Dispose();

            return false;
        }

        buffer.Dispose();
        _queue.Enqueue(buffer);
        return true;
    }
}