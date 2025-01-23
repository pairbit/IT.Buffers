#if NET
using System;
using System.Buffers;
using System.Collections.Generic;

namespace IT.Buffers.Extensions;

public static class xReadOnlySequence
{
    public static bool SequenceEqual<T>(this ReadOnlySequence<T> first, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
    {
        if (first.IsSingleSegment) return first.FirstSpan.SequenceEqual(other, comparer);

        if (first.Length == other.Length)
        {
            var position = first.Start;
            while (first.TryGet(ref position, out var memory))
            {
                var span = memory.Span;

                if (!span.SequenceEqual(other[..span.Length], comparer)) return false;

                other = other[span.Length..];
            }
        }

        return true;
    }
}
#endif