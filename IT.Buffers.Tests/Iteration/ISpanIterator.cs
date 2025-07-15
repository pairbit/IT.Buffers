using System;

namespace IT.Buffers.Iteration;

internal interface ISpanIterator<T>
{
    bool TryGetNextSpan(out Span<T> span);
}