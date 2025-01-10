using System.Collections.Concurrent;

namespace IT.Buffers.Pool;

public class LinkedBufferWriterPool<T>
{
    private static readonly ConcurrentQueue<LinkedBufferWriter<T>> _queue = new();

    public static LinkedBufferWriter<T> Rent()
    {
        if (_queue.TryDequeue(out var writer))
        {
            return writer;
        }
        return new LinkedBufferWriter<T>(useFirstBuffer: false);
    }

    public static void Return(LinkedBufferWriter<T> writer)
    {
        writer.Reset();
        _queue.Enqueue(writer);
    }
}