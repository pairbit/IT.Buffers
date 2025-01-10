using System.Collections.Concurrent;

namespace IT.Buffers.Pool;

public class LinkedBufferWriterPool<T>
{
    private static readonly ConcurrentQueue<LinkedBufferWriter<T>> _queue = new();

    public static LinkedBufferWriter<T> Rent(int bufferSize = 0)
    {
        if (_queue.TryDequeue(out var writer))
        {
            if (bufferSize > 0) writer.SetInitialBufferSize(bufferSize);
            return writer;
        }
        return bufferSize == 0
            ? new LinkedBufferWriter<T>()
            : new LinkedBufferWriter<T>(bufferSize);
    }

    public static void Return(LinkedBufferWriter<T> writer)
    {
        writer.Reset();
        _queue.Enqueue(writer);
    }
}