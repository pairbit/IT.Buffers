using System;

namespace IT.Buffers.Tests.Internal.Iteration;

internal interface IMemoryIterator<T> : ISpanIterator<T>
{
    bool TryGetNextMemory(out Memory<T> memory);
}