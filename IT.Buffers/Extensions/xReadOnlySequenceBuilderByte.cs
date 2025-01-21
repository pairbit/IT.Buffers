using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers.Extensions;

public static class xReadOnlySequenceBuilderByte
{
    public static ReadOnlySequenceBuilder<byte> Add(this ReadOnlySequenceBuilder<byte> builder,
        Stream stream, int bufferSize = BufferSize.KB_64)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var offset = 0;
        do
        {
            if (offset == buffer.Length)
            {
                builder.Add(buffer, returnToPool: true);
                buffer = ArrayPool<byte>.Shared.Rent(BufferSize.GetDoubleCapacity(buffer.Length));
                offset = 0;
            }

            int readed;
            try
            {
                readed = stream.Read(buffer.AsSpan(offset, buffer.Length - offset));
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }

            offset += readed;

            if (readed == 0)
            {
                builder.Add(buffer.AsMemory(0, offset), returnToPool: true);
                break;
            }
        } while (true);

        return builder;
    }

    public static async ValueTask<ReadOnlySequenceBuilder<byte>> AddAsync(this ReadOnlySequenceBuilder<byte> builder,
        Stream stream, int bufferSize = BufferSize.KB_64, CancellationToken cancellationToken = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var offset = 0;
        do
        {
            if (offset == buffer.Length)
            {
                builder.Add(buffer, returnToPool: true);
                buffer = ArrayPool<byte>.Shared.Rent(BufferSize.GetDoubleCapacity(buffer.Length));
                offset = 0;
            }

            int readed;
            try
            {
                readed = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }

            offset += readed;

            if (readed == 0)
            {
                builder.Add(buffer.AsMemory(0, offset), returnToPool: true);
                break;
            }
        } while (true);

        return builder;
    }
}