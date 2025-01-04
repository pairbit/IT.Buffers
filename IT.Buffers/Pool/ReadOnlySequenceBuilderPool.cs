using System;
using System.Collections.Concurrent;

namespace IT.Buffers.Pool;

public class ReadOnlySequenceBuilderPool<T>
{
    private static readonly ConcurrentQueue<ReadOnlySequenceBuilder<T>> _queue = new();

    public static ReadOnlySequenceBuilder<T> Rent(int capacity = 0)
    {
        if (_queue.TryDequeue(out var builder))
        {
            if (capacity > 0) builder.EnsureCapacity(capacity);
            return builder;
        }

        return capacity == 0
            ? new ReadOnlySequenceBuilder<T>()
            : new ReadOnlySequenceBuilder<T>(capacity);
    }

    public static void Return(ReadOnlySequenceBuilder<T> builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.Reset();
        _queue.Enqueue(builder);
    }
}