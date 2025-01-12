using System;
using System.Collections.Concurrent;

namespace IT.Buffers;

public class LinkedBufferWriterPool<T>
{
    private static readonly ConcurrentQueue<LinkedBufferWriter<T>> _queue = new();

    public static LinkedBufferWriter<T> Rent(int bufferSize = 0)
    {
        if (_queue.TryDequeue(out var writer))
        {
            if (bufferSize > 0) writer.GetSpan(bufferSize);
            return writer;
        }
        return bufferSize == 0
            ? new LinkedBufferWriter<T>()
            : new LinkedBufferWriter<T>(bufferSize);
    }

    public static void Return(LinkedBufferWriter<T> writer)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.Dispose();
        _queue.Enqueue(writer);
    }
}