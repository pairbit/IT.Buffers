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
        if (first.Length != other.Length) return false;

        var position = first.Start;
        while (first.TryGet(ref position, out var memory))
        {
            var span = memory.Span;

            if (!span.SequenceEqual(other[..span.Length], comparer)) return false;

            other = other[span.Length..];
        }

        return true;
    }

    public static bool SequenceEqual<T>(this ReadOnlySequence<T> first, ReadOnlySequence<T> other, IEqualityComparer<T>? comparer = null)
    {
        if (first.IsSingleSegment) return other.SequenceEqual(first.FirstSpan, comparer);
        if (other.IsSingleSegment) return first.SequenceEqual(other.FirstSpan, comparer);
        if (first.Length != other.Length) return false;

        var firstPosition = first.Start;
        var otherPosition = other.Start;
        ReadOnlySpan<T> firstSpan;
        ReadOnlySpan<T> otherSpan = default;
        while (first.TryGet(ref firstPosition, out var firstMemory))
        {
            firstSpan = firstMemory.Span;
            if (firstSpan.Length == 0) continue;

            if (otherSpan.Length > 0)
            {
                if (otherSpan.Length >= firstSpan.Length)
                {
                    if (!firstSpan.SequenceEqual(otherSpan[..firstSpan.Length])) return false;
                    otherSpan = otherSpan[firstSpan.Length..];
                    continue;
                }

                if (!firstSpan[..otherSpan.Length].SequenceEqual(otherSpan)) return false;
                firstSpan = firstSpan[otherSpan.Length..];
            }

            while (other.TryGet(ref otherPosition, out var otherMemory))
            {
                otherSpan = otherMemory.Span;
                if (otherSpan.Length == 0) continue;

                if (otherSpan.Length >= firstSpan.Length)
                {
                    if (!firstSpan.SequenceEqual(otherSpan[..firstSpan.Length])) return false;
                    otherSpan = otherSpan[firstSpan.Length..];
                    break;
                }

                if (!firstSpan[..otherSpan.Length].SequenceEqual(otherSpan)) return false;
                firstSpan = firstSpan[otherSpan.Length..];
            }
        }

        return true;
    }
}
#endif