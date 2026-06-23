#if NET10_0_OR_GREATER
using IT.Buffers.Extensions;
using System.Buffers;
using System.IO.Pipelines;

namespace IT.Buffers.Tests;

internal class PipeTest
{
    //[Test]
    public async Task Test()
    {
        var PageSize = 4096;
        var hal = PageSize >> 2;


        var hybridPool = new HybridArrayPool<byte>(HybridArrayPoolOptions.Create());
        var hybridMemoryPool = BufferPool.CreateMemoryPool(hybridPool);

        var pipeOptions = new PipeOptions(hybridMemoryPool,
            minimumSegmentSize: 4011);

        var pipe = new Pipe(pipeOptions);
        var writer = pipe.Writer;

        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);
        var stream = new MemoryStream(bytes);

        await writer.WriteAsync(stream);

        //await writer.FlushAsync();

        await writer.CompleteAsync();

        pipe.Reset();
    }
}
#endif