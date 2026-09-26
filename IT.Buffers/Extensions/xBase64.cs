using System.Diagnostics;

namespace System.Buffers.Text;

internal static class xBase64
{
    extension(Base64)
    {
        public static long EncodeToUtf8(ReadOnlySpan<byte> bytes, IBufferWriter<byte> bufferWriter)
        {
            if (bufferWriter == null) throw new ArgumentNullException(nameof(bufferWriter));

            long lengthLong = 0;

            while (bytes.Length > 0)
            {
                var status = Base64.EncodeToUtf8(bytes, bufferWriter.GetSpan(4), out var consumed, out var written);

                if (written > 0)
                {
                    bufferWriter.Advance(written);

                    lengthLong += written;

                    bytes = bytes.Slice(consumed);
                }
                else if (status == OperationStatus.DestinationTooSmall)
                {
                    throw new InvalidOperationException("DestinationTooSmall");
                }
                else if (status == OperationStatus.DestinationTooSmall)
                {
                    throw new InvalidOperationException("DestinationTooSmall");
                }

                if (status == OperationStatus.Done)
                    break;

                Debug.Assert(status == OperationStatus.DestinationTooSmall);
            }

            return lengthLong;
        }
    }
}