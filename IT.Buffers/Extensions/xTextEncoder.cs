#if NET
using System;
using System.Buffers;
using System.Diagnostics;
using System.Text.Encodings.Web;

namespace IT.Buffers.Extensions;

public static class xTextEncoder
{
    public static long EncodeUtf8(this TextEncoder textEncoder, ReadOnlySpan<byte> utf8Text, IBufferWriter<byte> bufferWriter)
    {
        ArgumentNullException.ThrowIfNull(textEncoder);
        ArgumentNullException.ThrowIfNull(bufferWriter);
        long lengthLong = 0;
        if (utf8Text.Length > 0)
        {
            var max = textEncoder.MaxOutputCharactersPerInputCharacter;
            do
            {
                var status = textEncoder.EncodeUtf8(utf8Text, bufferWriter.GetSpan(max), out var consumed, out var written);
                if (written > 0)
                {
                    bufferWriter.Advance(written);

                    lengthLong += written;

                    utf8Text = utf8Text.Slice(consumed);
                }
                else if (status == OperationStatus.DestinationTooSmall)
                {
                    Debug.Assert(consumed == 0);
                    throw new InvalidOperationException("DestinationTooSmall");
                }

                if (status == OperationStatus.Done) break;

                Debug.Assert(status == OperationStatus.DestinationTooSmall);
            } while (true);

            Debug.Assert(utf8Text.IsEmpty);
        }
        return lengthLong;
    }
}
#endif