using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace IT.Buffers;

internal readonly struct ReadOnlyBuffer<T>
{
    public static readonly ReadOnlyBuffer<T> Null;
    public static readonly ReadOnlyBuffer<T> Empty = new(Array.Empty<T>(), 0, 0);

    private readonly int _index;
    private readonly int _length;
    private readonly object? _buffer;

    private ReadOnlyBuffer(object? buffer, int index, int length)
    {
        _buffer = buffer;
        _index = index;
        _length = length;
    }

    public static ReadOnlyBuffer<T> FromRaw(ReadOnlySpan<T> span)
    {
        if (span.Length > 8)
            throw new ArgumentException("too long", nameof(span));

        throw new NotImplementedException();
    }

    public static ReadOnlyBuffer<T> FromMemory(ReadOnlyMemory<T> memory)
    {
        if (memory.IsEmpty)
            return Empty;

        if (MemoryMarshal.TryGetArray(memory, out var segment))
            return new(segment.Array, segment.Offset, segment.Count);

        if (MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out var manager, out var start, out var length))
            return new(manager, start, length);

        throw new ArgumentException("Unrecognized memory type", nameof(memory));
    }

    public static ReadOnlyBuffer<T> FromSequence(ReadOnlySequence<T> sequence)
    {
        if (sequence.IsSingleSegment) return FromMemory(sequence.First);
        if (sequence.Length > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sequence));

        var pos = sequence.Start;
        var segment = pos.GetObject() ?? throw new ArgumentException("StartSegment is null", nameof(sequence));
        return new((ReadOnlySequenceSegment<byte>)segment, pos.GetInteger(), checked((int)sequence.Length));
    }

    public static ReadOnlyBuffer<char> FromString(string str)
    {
        if (str is null)
            return ReadOnlyBuffer<char>.Null;

        return new ReadOnlyBuffer<char>(str, 0, str.Length);
    }
}