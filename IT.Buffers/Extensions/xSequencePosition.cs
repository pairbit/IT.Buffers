using System;

namespace IT.Buffers.Extensions;

internal static class xSequencePosition
{
    public static bool IsEnd(this SequencePosition position)
        => position.GetObject() == null;

    public static bool IsNegative(this SequencePosition position)
        => position.GetInteger() < 0;

    public static bool IsEmpty(this SequencePosition position)
        => position.GetInteger() == 0 && position.GetObject() == null;
}