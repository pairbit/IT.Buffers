using System;
using System.Buffers;
using System.Collections.Generic;

namespace IT.Buffers;

public class ReadOnlySequenceBuilder<T>
{
    private readonly Stack<SequenceSegment<T>> _pool;
    private readonly List<SequenceSegment<T>> _list;

    public ReadOnlySequenceBuilder()
    {
        _pool = new Stack<SequenceSegment<T>>();
        _list = [];
    }

    public ReadOnlySequenceBuilder(int poolCapacity = 0, int capacity = 0)
    {
        _pool = new Stack<SequenceSegment<T>>(poolCapacity);
        _list = new(capacity);
    }

#if NET6_0_OR_GREATER
    public void EnsureCapacity(int capacity)
    {
        _pool.EnsureCapacity(capacity);
        _list.EnsureCapacity(capacity);
    }
#endif

    public void Add(ReadOnlyMemory<T> buffer, bool returnToPool = false)
    {
        if (!_pool.TryPop(out var segment))
        {
            segment = new SequenceSegment<T>();
        }

        segment.SetBuffer(buffer, returnToPool);
        _list.Add(segment);
    }

    public bool TryGetSingleMemory(out ReadOnlyMemory<T> memory)
    {
        if (_list.Count == 1)
        {
            memory = _list[0].Memory;
            return true;
        }
        memory = default;
        return false;
    }

    public ReadOnlySequence<T> Build()
    {
        if (_list.Count == 0) return ReadOnlySequence<T>.Empty;

        if (_list.Count == 1) return new ReadOnlySequence<T>(_list[0].Memory);

        long running = 0;
#if NET7_0_OR_GREATER
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_list);
        var lastIndex = span.Length - 1;
        for (int i = 0; i < lastIndex; i++)
        {
            var segment = span[i];
            segment.SetRunningIndexAndNext(running, span[i + 1]);
            running += segment.Memory.Length;
        }
        var firstSegment = span[0];
        var lastSegment = span[lastIndex];
#else
        var list = _list;
        var lastIndex = list.Count - 1;
        for (int i = 0; i < lastIndex; i++)
        {
            var segment = list[i];
            segment.SetRunningIndexAndNext(running, list[i + 1]);
            running += segment.Memory.Length;
        }
        var firstSegment = list[0];
        var lastSegment = list[lastIndex];
#endif
        lastSegment.SetRunningIndexAndNext(running, null);
        return new ReadOnlySequence<T>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
    }

    public void Reset()
    {
#if NET7_0_OR_GREATER
        var segments = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_list);
#else
        var segments = _list;
#endif
        foreach (var segment in segments)
        {
            segment.Reset();
            _pool.Push(segment);
        }
        _list.Clear();
    }
}