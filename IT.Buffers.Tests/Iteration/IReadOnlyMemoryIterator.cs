using System;

namespace IT.Buffers.Iteration;

internal interface IReadOnlyMemoryIterator<T> : IReadOnlySpanIterator<T>
{
    bool TryGetNextMemory(out ReadOnlyMemory<T> memory);
}