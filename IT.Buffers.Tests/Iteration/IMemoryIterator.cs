using System;

namespace IT.Buffers.Iteration;

internal interface IMemoryIterator<T> : ISpanIterator<T>
{
    bool TryGetNextMemory(out Memory<T> memory);
}