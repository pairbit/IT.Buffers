namespace IT.Buffers.Tests;

public class ValueBufferWriterTest
{
    [Test]
    public void Test()
    {
        var writer = new ValueRentedBufferWriter<byte>();

        try
        {
            Test(ref writer);
        }
        finally
        {
            Assert.That(writer.Written, Is.Not.EqualTo(0));
            writer.Dispose();
        }
    }

    private static void Test<TBufferWriter>(ref TBufferWriter writer)
        where TBufferWriter : IAdvancedBufferWriter<byte>
    {
        Assert.That(writer.Written, Is.EqualTo(0));
        Assert.That(writer.WrittenLong, Is.EqualTo(0));

        var span = writer.GetSpan();

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        Assert.That(writer.Written, Is.EqualTo(span.Length));
        Assert.That(writer.WrittenLong, Is.EqualTo(span.Length));
    }
}