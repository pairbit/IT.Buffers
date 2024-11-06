using System;
using System.Buffers;
using System.Collections.Generic;

namespace IT.Buffers;

public class ReadOnlySequenceBuilder<T>
{
    private readonly Stack<SequenceSegment<T>> segmentPool;
    private readonly List<SequenceSegment<T>> list;

    public ReadOnlySequenceBuilder()
    {
        list = new();
        segmentPool = new Stack<SequenceSegment<T>>();
    }

    public void Add(ReadOnlyMemory<T> buffer, bool returnToPool)
    {
        if (!segmentPool.TryPop(out var segment))
        {
            segment = new SequenceSegment<T>();
        }

        segment.SetBuffer(buffer, returnToPool);
        list.Add(segment);
    }

    public bool TryGetSingleMemory(out ReadOnlyMemory<T> memory)
    {
        if (list.Count == 1)
        {
            memory = list[0].Memory;
            return true;
        }
        memory = default;
        return false;
    }

    public ReadOnlySequence<T> Build()
    {
        if (list.Count == 0)
        {
            return ReadOnlySequence<T>.Empty;
        }

        if (list.Count == 1)
        {
            return new ReadOnlySequence<T>(list[0].Memory);
        }

        long running = 0;
#if NET7_0_OR_GREATER
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
        for (int i = 0; i < span.Length; i++)
        {
            var next = i < span.Length - 1 ? span[i + 1] : null;
            span[i].SetRunningIndexAndNext(running, next);
            running += span[i].Memory.Length;
        }
        var firstSegment = span[0];
        var lastSegment = span[span.Length - 1];
#else
        var span = list;
        for (int i = 0; i < span.Count; i++)
        {
            var next = i < span.Count - 1 ? span[i + 1] : null;
            span[i].SetRunningIndexAndNext(running, next);
            running += span[i].Memory.Length;
        }
        var firstSegment = span[0];
        var lastSegment = span[span.Count - 1];
#endif
        return new ReadOnlySequence<T>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
    }

    public void Reset()
    {
#if NET7_0_OR_GREATER
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
#else
        var span = list;
#endif
        foreach (var item in span)
        {
            item.Reset();
            segmentPool.Push(item);
        }
        list.Clear();
    }
}