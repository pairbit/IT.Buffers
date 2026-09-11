using IT.Buffers.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace IT.Buffers;

public class BufferPool<TBuffer> : IBufferPool<TBuffer> where TBuffer : class, IDisposable, new()
{
    public static readonly BufferPool<TBuffer> Shared = new();

    private readonly ConcurrentQueue<TBuffer> _queue;
    private readonly int _id;

    public int Id => _id;

    private BufferPool()
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

    public TBuffer Rent()
    {
        if (!_queue.TryDequeue(out var buffer))
        {
            buffer = new TBuffer();
            if (buffer is IRentedBuffer<TBuffer> rentedBuffer)
            {
                rentedBuffer.SetBufferPool(this);
#if DEBUG
                try
                {
                    rentedBuffer.SetBufferPool(this);
                    Debug.Fail("The buffer pool can be set only once.");
                }
                catch (InvalidOperationException ex)
                {
                    Debug.Assert(ex.Message == "The buffer pool can be set only once.");
                }
#endif
                return buffer;
            }
        }

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

    public void Return(TBuffer buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

        if (buffer is IRentedBuffer<TBuffer> rentedBuffer)
        {
            var bufferPool = rentedBuffer.BufferPool;
            if (bufferPool == null)
                throw new ArgumentException("Buffer not rented.", nameof(buffer));

            if (!ReferenceEquals(this, bufferPool))
            {
                var id = bufferPool.Id;
                throw new ArgumentException(_id == id ?
                    $"The buffer is rented from another pool." :
                    $"The buffer is rented from the pool with id {id}."
                    , nameof(buffer));
            }
        }

        _queue.Enqueue(buffer);
    }
}