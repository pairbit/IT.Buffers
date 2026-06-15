using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers.Extensions;

public static class xBufferWriter
{
    public static T[] ToArrayAndReset<T>(this BufferWriter<T> writer)
    {
        var written = writer.Written;
        if (written > 0)
        {
            var array =
#if NET5_0_OR_GREATER
                System.GC.AllocateUninitializedArray<T>(written);
#else
                new T[written];
#endif
            writer.TryWriteToAndReset(array);

            return array;
        }

        writer.Reset();
        return [];
    }

    public static async ValueTask WriteToAndResetAsync(this BufferWriter<byte> writer, Stream stream, CancellationToken cancellationToken = default)
    {
        if (writer._written > 0)
        {
            var buffers = writer._buffers;
            if (buffers.Count > 0)
            {
                foreach (var item in buffers)
                {
                    Debug.Assert(item.Written > 0);
                    await stream.WriteAsync(item.WrittenMemory, cancellationToken).ConfigureAwait(false);
                    item.Reset(writer._arrayPool);
                }
            }

            var current = writer._current;
            if (!current.IsNull)
            {
                Debug.Assert(current.Written > 0);
                await stream.WriteAsync(current.WrittenMemory, cancellationToken).ConfigureAwait(false);
                current.Reset(writer._arrayPool);
            }

            writer.ResetCore();
        }
        else
        {
            writer.Reset();
        }
    }
}