namespace IT.Buffers.Tests;

public class LinkedBufferWriterTest
{
    [Test]
    public void Test()
    {
        var writer = LinkedBufferWriterPool<byte>.Rent(1);

        var span = writer.GetSpan();

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        span = writer.GetSpan();

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        LinkedBufferWriterPool<byte>.Return(writer);
    }
}