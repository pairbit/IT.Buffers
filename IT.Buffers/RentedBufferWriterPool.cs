using System;
using System.Collections.Concurrent;

namespace IT.Buffers;

public static class RentedBufferWriterPool<T>
{
    private static readonly ConcurrentQueue<RentedBufferWriter<T>> _queue = new();

    public static RentedBufferWriter<T> Rent(int capacity = 0)
    {
        if (_queue.TryDequeue(out var writer))
        {
            writer.Initialize(capacity);
            return writer;
        }

        return new RentedBufferWriter<T>(capacity);
    }

    public static void Return(RentedBufferWriter<T> writer)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.Dispose();
        _queue.Enqueue(writer);
    }
}