#if NET
using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace IT.Buffers.Extensions;

public static class xReadOnlySequence
{
    public static SequencePosition PositionOf<T>(this in ReadOnlySequence<T> sequence, ReadOnlySpan<T> value) 
        where T : IEquatable<T>
#if NET7_0_OR_GREATER
        ?
#endif
    {
        var current = sequence.Start;
        var next = current;
        var valueLength = value.Length;
        SequencePosition position = default;
        int valueLengthPart = 0;
        while (sequence.TryGet(ref next, out var memory))
        {
            var spanLength = memory.Length;
            if (spanLength == 0)
            {
                current = next;
                continue;
            }

            var span = memory.Span;
            if (valueLengthPart > 0)
            {
                Debug.Assert(valueLength > valueLengthPart);

                var remainder = valueLength - valueLengthPart;
                if (remainder > spanLength)
                {
                    if (span.SequenceEqual(value.Slice(valueLengthPart, spanLength)))
                    {
                        valueLengthPart += spanLength;
                        current = next;
                        continue;
                    }
                }
                else if (remainder == spanLength)
                {
                    if (span.SequenceEqual(value.Slice(valueLengthPart)))
                    {
                        return position;
                    }
                }
                else if (span.StartsWith(value.Slice(valueLengthPart)))
                {
                    return position;
                }
            }

            var index = span.IndexOfPart(value, out valueLengthPart);
            if (index > -1)
            {
                position = new(current.GetObject(), index);

                if (valueLength == valueLengthPart) 
                    return position;
            }
            current = next;
        }
        return new(null, -1);
    }

    public static SequencePosition EndPositionOf<T>(this in ReadOnlySequence<T> sequence, ReadOnlySpan<T> value)
            where T : IEquatable<T>
#if NET7_0_OR_GREATER
        ?
#endif
    {
        var current = sequence.Start;
        var next = current;
        var valueLength = value.Length;
        int valueLengthPart = 0;
        while (sequence.TryGet(ref next, out var memory))
        {
            var spanLength = memory.Length;
            if (spanLength == 0)
            {
                current = next;
                continue;
            }

            var span = memory.Span;
            if (valueLengthPart > 0)
            {
                Debug.Assert(valueLength > valueLengthPart);

                var remainder = valueLength - valueLengthPart;
                if (remainder > spanLength)
                {
                    if (span.SequenceEqual(value.Slice(valueLengthPart, spanLength)))
                    {
                        valueLengthPart += spanLength;
                        current = next;
                        continue;
                    }
                }
                else if (remainder == spanLength)
                {
                    if (span.SequenceEqual(value.Slice(valueLengthPart)))
                    {
                        return new(current.GetObject(), spanLength);
                    }
                }
                else if (span.StartsWith(value.Slice(valueLengthPart)))
                {
                    return new(current.GetObject(), spanLength);
                }
            }

            var index = span.IndexOfPart(value, out valueLengthPart);
            if (index > -1)
            {
                if (valueLength == valueLengthPart)
                    return new(current.GetObject(), index + valueLength);
            }
            current = next;
        }
        return new(null, -1);
    }

    public static bool SequenceEqual<T>(this in ReadOnlySequence<T> first, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
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

    public static bool SequenceEqual<T>(this in ReadOnlySequence<T> first, in ReadOnlySequence<T> other, IEqualityComparer<T>? comparer = null)
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
                    if (!firstSpan.SequenceEqual(otherSpan[..firstSpan.Length], comparer)) return false;
                    otherSpan = otherSpan[firstSpan.Length..];
                    continue;
                }

                if (!firstSpan[..otherSpan.Length].SequenceEqual(otherSpan, comparer)) return false;
                firstSpan = firstSpan[otherSpan.Length..];
            }

            while (other.TryGet(ref otherPosition, out var otherMemory))
            {
                otherSpan = otherMemory.Span;
                if (otherSpan.Length == 0) continue;

                if (otherSpan.Length >= firstSpan.Length)
                {
                    if (!firstSpan.SequenceEqual(otherSpan[..firstSpan.Length], comparer)) return false;
                    otherSpan = otherSpan[firstSpan.Length..];
                    break;
                }

                if (!firstSpan[..otherSpan.Length].SequenceEqual(otherSpan, comparer)) return false;
                firstSpan = firstSpan[otherSpan.Length..];
            }
        }

        return true;
    }
}
#endif