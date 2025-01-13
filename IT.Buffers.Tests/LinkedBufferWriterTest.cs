namespace IT.Buffers.Tests;

public class LinkedBufferWriterTest
{
    [Test]
    public void Test()
    {
        var writer = LinkedBufferWriter<byte>.Pool.Rent();

        try
        {
            Test(writer);
        }
        finally
        {
            LinkedBufferWriter<byte>.Pool.Return(writer);
        }
    }

    private void Test(LinkedBufferWriter<byte> writer)
    {
        var span = writer.GetSpan();

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        span = writer.GetSpan();

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);
    }
}