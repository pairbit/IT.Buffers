using System;

namespace IT.Buffers.Tests.Internal.Iteration;

internal interface IReadOnlySpanIterator<T>
{
    bool TryGetNextSpan(out ReadOnlySpan<T> span);
}