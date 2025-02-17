﻿using System;
using System.Buffers;
using System.Collections.Generic;

namespace IT.Buffers;

public sealed class ReadOnlySequenceBuilder<T> : IDisposable
{
    public static BufferPool<ReadOnlySequenceBuilder<T>> Pool
        => BufferPool<ReadOnlySequenceBuilder<T>>.Shared;

    private Stack<SequenceSegment<T>>? _stack;
    private readonly List<SequenceSegment<T>> _list;

    public int Count => _list.Count;

    public int Capacity
    {
        get => _list.Capacity;
        set => _list.Capacity = value;
    }

    public ReadOnlySequenceBuilder()
    {
        _list = [];
    }

    public ReadOnlySequenceBuilder(int capacity)
    {
        _list = new(capacity);
    }

    public void EnsureCapacity(int capacity)
    {
        _list.EnsureCapacity(capacity);
    }

    public ReadOnlySequenceBuilder<T> Add(ReadOnlyMemory<T> memory, bool isRented = false)
    {
        if (_stack == null || !_stack.TryPop(out var segment))
        {
            segment = new SequenceSegment<T>();
        }

        segment.SetMemory(memory, isRented);
        _list.Add(segment);

        return this;
    }

    public ReadOnlySequenceBuilder<T> Add(ReadOnlyMemory<T> memory, int maxSegments, bool isRented = false)
    {
        if (maxSegments <= 0) throw new ArgumentOutOfRangeException(nameof(maxSegments));

        var length = memory.Length;
        var segments = length < maxSegments ? length : maxSegments;

        if (segments > 1)
        {
            _list.EnsureCapacity(_list.Count + segments);

            var segmentLength = length / segments;

            for (int i = segments - 2; i >= 0; i--)
            {
                Add(memory[..segmentLength]);

                memory = memory[segmentLength..];
            }
        }

        Add(memory, isRented);

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

        if (_list.Count == 1)
        {
            var single = _list[0];
            return new ReadOnlySequence<T>(single, 0, single, single.Memory.Length);
        }

        long running = 0;
#if NET6_0_OR_GREATER
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_list);
        var lastIndex = span.Length - 1;
        for (int i = 0; i < lastIndex; i++)
        {
            var segment = span[i];
            segment.RunningIndex = running;
            segment.Next = span[i + 1];
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
            segment.RunningIndex = running;
            segment.Next = list[i + 1];
            running += segment.Memory.Length;
        }
        var firstSegment = list[0];
        var lastSegment = list[lastIndex];
#endif
        lastSegment.RunningIndex = running;
        return new ReadOnlySequence<T>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
    }

    public void Reset()
    {
        if (_list.Count == 0) return;

#if NET6_0_OR_GREATER
        var segments = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_list);
#else
        var segments = _list;
#endif
        var stack = _stack;
        if (stack == null)
        {
            stack = _stack = new Stack<SequenceSegment<T>>(_list.Capacity);
        }
#if NET6_0_OR_GREATER
        else
        {
            stack.EnsureCapacity(_list.Capacity);
        }
#endif
        foreach (var segment in segments)
        {
            segment.Reset();
            stack.Push(segment);
        }
        _list.Clear();
    }

    void IDisposable.Dispose() => Reset();
}