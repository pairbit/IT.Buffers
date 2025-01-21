using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers.Extensions;

public static class xLinkedBufferWriterByte
{
    public static async ValueTask WriteAndResetAsync(this LinkedBufferWriter<byte> writer, Stream stream, CancellationToken cancellationToken)
    {
        if (writer._written == 0) return;

        var firstBufferWritten = writer._firstBufferWritten;
        if (firstBufferWritten > 0)
        {
            Debug.Assert(writer._firstBuffer.Length >= firstBufferWritten);
            await stream.WriteAsync(writer._firstBuffer.AsMemory(0, firstBufferWritten), cancellationToken).ConfigureAwait(false);
        }

        var buffers = writer._buffers;
        if (buffers.Count > 0)
        {
            foreach (var item in buffers)
            {
                Debug.Assert(item.Written > 0);
                await stream.WriteAsync(item.WrittenMemory, cancellationToken).ConfigureAwait(false);
                item.Reset();
            }
        }

        var current = writer._current;
        if (!current.IsNull)
        {
            Debug.Assert(current.Written > 0);
            await stream.WriteAsync(current.WrittenMemory, cancellationToken).ConfigureAwait(false);
            current.Reset();
        }

        writer.ResetCore();
    }
}