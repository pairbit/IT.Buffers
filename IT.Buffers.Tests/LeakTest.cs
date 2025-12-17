namespace IT.Buffers.Tests;

internal class LeakTest
{
    [Test]
    public void Test()
    {
        var writer = BufferWriter<byte>.Pool.Rent();
        try
        {
            writer.GetSpan(BufferSize.KB);
        }
        finally
        {
            //leak because (_written == 0)
            BufferPool.TryReturn(writer);
        }
    }
}