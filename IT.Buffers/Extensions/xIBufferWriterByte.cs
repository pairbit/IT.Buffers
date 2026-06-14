using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers.Extensions;

public static class xIBufferWriterByte
{
    public static async Task WriteAsync(this IBufferWriter<byte> writer, Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        do
        {
            var memory = writer.GetMemory();
            int read = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;

            writer.Advance(read);

        } while (true);
    }
}