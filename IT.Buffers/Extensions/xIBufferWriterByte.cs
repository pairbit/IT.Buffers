using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers.Extensions;

public static class xIBufferWriterByte
{
    public static async Task WriteAsync(this IBufferWriter<byte> writer, Stream stream, CancellationToken cancellationToken)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        do
        {
            int read = await stream.ReadAsync(writer.GetMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;

            writer.Advance(read);

        } while (true);
    }
}