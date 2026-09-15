using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace IT.Buffers;

public class ArrayBufferWriterPool<T> : IBufferPool<ArrayBufferWriter<T>>
{
    public static readonly ArrayBufferWriterPool<T> Shared = new();

    private readonly ConcurrentQueue<ArrayBufferWriter<T>> _queue = new();

    public int Id => 0;

    public Type BufferType => typeof(ArrayBufferWriterPool<T>);

    public bool TryRent([MaybeNullWhen(false)] out ArrayBufferWriter<T> buffer) =>
        _queue.TryDequeue(out buffer);

    public ArrayBufferWriter<T> Rent() =>
        _queue.TryDequeue(out var buffer) ? buffer : new ArrayBufferWriter<T>();

    public bool TryReturn(ArrayBufferWriter<T> buffer)
    {
        Return(buffer);

        return true;
    }

    public void Return(ArrayBufferWriter<T> buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
#if NET8_0_OR_GREATER
        if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            buffer.Clear();
        else
            buffer.ResetWrittenCount();
#else
        buffer.Clear();
#endif
        _queue.Enqueue(buffer);
    }
}