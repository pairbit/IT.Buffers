using System;
using System.Collections.Concurrent;

namespace IT.Buffers;

public class ReadOnlySequenceBuilderPool<T>
{
    private static readonly ConcurrentQueue<ReadOnlySequenceBuilder<T>> _queue = new();

    public static ReadOnlySequenceBuilder<T> Rent()
    {
        if (_queue.TryDequeue(out var builder)) return builder;

        return new ReadOnlySequenceBuilder<T>();
    }

    public static void Return(ReadOnlySequenceBuilder<T> builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.Reset();
        _queue.Enqueue(builder);
    }
}