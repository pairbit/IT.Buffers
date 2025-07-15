using System;

namespace IT.Buffers.Tests.Internal.Iteration;

internal interface IReadOnlyMemoryIterator<T> : IReadOnlySpanIterator<T>
{
    bool TryGetNextMemory(out ReadOnlyMemory<T> memory);
}