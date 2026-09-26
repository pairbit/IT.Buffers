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

            if (bytes.Length > 0)
            {
                do
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
                        Debug.Assert(consumed == 0);
                        throw new InvalidOperationException("DestinationTooSmall");
                    }

                    if (status == OperationStatus.Done) break;

                    Debug.Assert(status == OperationStatus.DestinationTooSmall);
                } while (true);

                Debug.Assert(bytes.IsEmpty);
            }

            return lengthLong;
        }
    }
}