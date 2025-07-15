using System;

namespace IT.Buffers.Tests.Internal.Iteration;

internal interface IMemoryIterator<T> : ISpanIterator<T>, IReadOnlyMemoryIterator<T>
{
    bool TryGetNextMemory(out Memory<T> memory);
}