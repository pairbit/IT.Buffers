using System.Collections.Concurrent;

namespace IT.Buffers.Pool;

public class LinkedBufferWriterPool
{
    private static readonly ConcurrentQueue<LinkedBufferWriter> _queue = new();

    public static LinkedBufferWriter Rent()
    {
        if (_queue.TryDequeue(out var writer))
        {
            return writer;
        }
        return new LinkedBufferWriter(useFirstBuffer: false);
    }

    public static void Return(LinkedBufferWriter writer)
    {
        writer.Reset();
        _queue.Enqueue(writer);
    }
}