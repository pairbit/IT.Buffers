using System;
using System.Buffers;

namespace IT.Buffers.Iteration;

internal struct ReadOnlySequenceIterator<T> : IReadOnlyMemoryIterator<T>
{
    private readonly ReadOnlySequence<T> _sequence;
    private SequencePosition _position;

    public ReadOnlySequenceIterator(in ReadOnlySequence<T> sequence)
    {
        _sequence = sequence;
        _position = sequence.Start;
    }

    public bool TryGetNextMemory(out ReadOnlyMemory<T> memory)
        => _sequence.TryGet(ref _position, out memory);

    public bool TryGetNextSpan(out ReadOnlySpan<T> span)
    {
        if (_sequence.TryGet(ref _position, out var memory))
        {
            span = memory.Span;
            return true;
        }
        span = default;
        return false;
    }

    public void Reset()
    {
        _position = _sequence.Start;
    }
}