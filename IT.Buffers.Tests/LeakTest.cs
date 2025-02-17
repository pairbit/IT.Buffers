namespace IT.Buffers.Tests;

internal class LeakTest
{
    [Test]
    public void Test()
    {
        var writer = LinkedBufferWriter<byte>.Pool.Rent();
        try
        {
            //writer.GetSpan(BufferSize.KB_16);
        }
        finally
        {
            BufferPool.TryReturn(writer);
        }
    }
}