using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace IT.Buffers.Pool;

public static class ArrayBufferWriterPool<T>
{
    [ThreadStatic]
    private static ArrayBufferWriter<T>? _writer;

    private static readonly ConcurrentQueue<ArrayBufferWriter<T>> _queue = new();

    public static ArrayBufferWriter<T> GetThreadStaticInstance()
    {
        var writer = _writer;
        if (writer == null)
        {
            writer = _writer = new ArrayBufferWriter<T>();
        }
#if NET8_0_OR_GREATER
        writer.ResetWrittenCount();
#else
        writer.Clear();
#endif
        return writer;
    }

    public static ArrayBufferWriter<T> Rent(int capacity = BufferSize.Min)
    {
        if (_queue.TryDequeue(out var writer))
        {
            //resize
            if (capacity > 0) writer.GetMemory(capacity);
            return writer;
        }

        return capacity == 0
            ? new ArrayBufferWriter<T>()
            : new ArrayBufferWriter<T>(capacity);
    }

    public static void Return(ArrayBufferWriter<T> writer)
    {
#if NET8_0_OR_GREATER
        writer.ResetWrittenCount();
#else
        writer.Clear();
#endif
        _queue.Enqueue(writer);
    }
}