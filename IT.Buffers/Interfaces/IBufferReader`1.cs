using System;

namespace IT.Buffers;

internal interface IBufferReader<T>
{
    void Advance(int count);

    ReadOnlyMemory<T> GetMemory(int sizeHint = 0);

    ReadOnlySpan<T> GetSpan(int sizeHint = 0);
}