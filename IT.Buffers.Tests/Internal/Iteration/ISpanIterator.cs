using System;

namespace IT.Buffers.Tests.Internal.Iteration;

internal interface ISpanIterator<T>
{
    bool TryGetNextSpan(out Span<T> span);
}