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
        return PositionOf(sequence, value, sequence.Start);
    }

    public static SequencePosition PositionOfEnd<T>(this in ReadOnlySequence<T> sequence, ReadOnlySpan<T> value)
        where T : IEquatable<T>
#if NET7_0_OR_GREATER
        ?
#endif
    {
        return PositionOfEnd(sequence, value, sequence.Start);
    }

    public static SequencePosition PositionOf<T>(this in ReadOnlySequence<T> sequence, ReadOnlySpan<T> value, SequencePosition start)
        where T : IEquatable<T>
#if NET7_0_OR_GREATER
        ?
#endif
    {
        var current = start;
        var valueLength = value.Length;
        SequencePosition position = default;
        int valueLengthPart = 0;
        for (var next = current; sequence.TryGet(ref next, out var memory); current = next)
        {
            var spanLength = memory.Length;
            if (spanLength == 0) continue;

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
                position = current.AddOffset(index);

                if (valueLength == valueLengthPart)
                    return position;
            }
        }
        return new(null, -1);
    }

    public static SequencePosition PositionOfEnd<T>(this in ReadOnlySequence<T> sequence, ReadOnlySpan<T> value, SequencePosition start)
        where T : IEquatable<T>
#if NET7_0_OR_GREATER
        ?
#endif
    {
        var current = start;
        var valueLength = value.Length;
        int valueLengthPart = 0;
        for (var next = current; sequence.TryGet(ref next, out var memory); current = next)
        {
            var spanLength = memory.Length;
            if (spanLength == 0) continue;

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
                        continue;
                    }
                }
                else if (remainder == spanLength)
                {
                    if (span.SequenceEqual(value.Slice(valueLengthPart)))
                    {
                        return new(current.GetObject(), remainder);
                        //return next.IsEnd() ? new(current.GetObject(), remainder) : next;
                    }
                }
                else if (span.StartsWith(value.Slice(valueLengthPart)))
                {
                    return new(current.GetObject(), remainder);
                }
            }

            var index = span.IndexOfPart(value, out valueLengthPart);
            if (index > -1)
            {
                if (valueLength == valueLengthPart)
                    return new(current.GetObject(), index + valueLength);
            }
        }
        return new(null, -1);
    }

#if NET
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
#endif
}