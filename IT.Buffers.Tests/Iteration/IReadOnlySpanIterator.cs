using System;

namespace IT.Buffers.Iteration;

internal interface IReadOnlySpanIterator<T>
{
    bool TryGetNextSpan(out ReadOnlySpan<T> span);
}