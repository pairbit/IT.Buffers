#if NET
using System.Buffers;
using System.Diagnostics;

namespace System.Text.Encodings.Web;

internal static class xTextEncoder
{
    public static long EncodeUtf8(this TextEncoder textEncoder, ReadOnlySpan<byte> utf8Text, IBufferWriter<byte> bufferWriter)
    {
        ArgumentNullException.ThrowIfNull(textEncoder);
        ArgumentNullException.ThrowIfNull(bufferWriter);
        long length = 0;
        do
        {
            var status = textEncoder.EncodeUtf8(utf8Text, bufferWriter.GetSpan(textEncoder.MaxOutputCharactersPerInputCharacter), out var consumed, out var written);
            if (written > 0)
            {
                bufferWriter.Advance(written);
                length += written;
                utf8Text = utf8Text.Slice(consumed);

                if (status == OperationStatus.Done) break;
            }
            else if (status == OperationStatus.DestinationTooSmall)
            {
                throw new InvalidOperationException("DestinationTooSmall");
            }
        } while (true);

        Debug.Assert(utf8Text.IsEmpty);

        return length;
    }
}
#endif