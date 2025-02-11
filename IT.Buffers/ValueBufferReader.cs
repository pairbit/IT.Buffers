using IT.Buffers.Interfaces;
using System;
using System.Buffers;

namespace IT.Buffers;

internal ref struct ValueBufferReader<T> : IBufferReader<T>
{
    private ReadOnlySequence<T> _sequence;
    private ReadOnlyMemory<T> _memory;
    private ReadOnlySpan<T> _span;

    public int Consumed { get; }

    public long ConsumedLong { get; }

    public bool HasMemory { get; }

    public bool HasSequence { get; }

    public ValueBufferReader(in ReadOnlySequence<T> sequence)
    {
        _sequence = sequence;
    }

    public ValueBufferReader(ReadOnlyMemory<T> memory)
    {
        _memory = memory;
    }

    public ValueBufferReader(ReadOnlySpan<T> span)
    {
        _span = span;
    }

    public void Advance(int count)
    {
        throw new NotImplementedException();
    }

    public ReadOnlyMemory<T> GetMemory(int sizeHint = 0)
    {
        throw new NotImplementedException();
    }

    public ReadOnlySpan<T> GetSpan(int sizeHint = 0)
    {
        throw new NotImplementedException();
    }
}