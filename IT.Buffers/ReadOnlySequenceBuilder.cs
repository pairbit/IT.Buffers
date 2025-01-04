﻿using System;
using System.Buffers;
using System.Collections.Generic;

namespace IT.Buffers;

public class ReadOnlySequenceBuilder<T>
{
    private Stack<SequenceSegment<T>>? _pool;
    private readonly List<SequenceSegment<T>> _list;

    public ReadOnlySequenceBuilder()
    {
        _list = [];
    }

    public ReadOnlySequenceBuilder(int capacity)
    {
        _list = new(capacity);
    }

#if NET6_0_OR_GREATER
    public void EnsureCapacity(int capacity)
    {
        _list.EnsureCapacity(capacity);
    }
#endif

    public ReadOnlySequenceBuilder<T> Add(ReadOnlyMemory<T> memory, bool returnToPool = false)
    {
        if (_pool == null || !_pool.TryPop(out var segment))
        {
            segment = new SequenceSegment<T>();
        }

        segment.SetBuffer(memory, returnToPool);
        _list.Add(segment);

        return this;
    }

    public ReadOnlySequenceBuilder<T> Add(ReadOnlyMemory<T> memory, int segments)
    {
        if (segments < 0) throw new ArgumentOutOfRangeException(nameof(segments));

        if (segments > 1)
        {
#if NET6_0_OR_GREATER
            _list.EnsureCapacity(_list.Count + segments);
#endif

            var segmentLength = memory.Length / segments;

            for (int i = segments - 2; i >= 0; i--)
            {
                Add(memory.Slice(0, segmentLength));

                memory = memory.Slice(segmentLength);
            }
        }

        Add(memory);

        return this;
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
        if (_list.Count == 0) return;

#if NET7_0_OR_GREATER
        var segments = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_list);
#else
        var segments = _list;
#endif
        var pool = _pool;
        if (pool == null)
        {
            pool = _pool = new Stack<SequenceSegment<T>>(_list.Capacity);
        }
#if NET6_0_OR_GREATER
        else
        {
            pool.EnsureCapacity(pool.Count + _list.Count);
        }
#endif
        foreach (var segment in segments)
        {
            segment.Reset();
            pool.Push(segment);
        }
        _list.Clear();
    }
}