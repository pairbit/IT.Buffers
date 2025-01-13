using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace IT.Buffers.Extensions;

public static class xBufferWriter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSpan<TBufferWriter, T>(ref TBufferWriter writer, ReadOnlySpan<T> span)
        where TBufferWriter : IBufferWriter<T>
    {
        Span<T> dest;
        int destlen, len;
        do
        {
            dest = writer.GetSpan();

            destlen = dest.Length;
            if (destlen == 0) throw new ArgumentOutOfRangeException(nameof(writer));

            len = span.Length;
            if (destlen >= len)
            {
                span.CopyTo(dest);
                writer.Advance(len);
                return;
            }

            span[..destlen].CopyTo(dest);
            writer.Advance(destlen);

            span = span[destlen..];
        } while (true);
    }
}