using IT.Buffers.Interfaces;

namespace IT.Buffers.Tests;

public class ValueBufferWriterTest
{
    [Test]
    public void Test()
    {
        ValueRentedBufferWriter<byte> writer = default;

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

        var span = writer.GetSpan();

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        Assert.That(writer.Written, Is.EqualTo(span.Length));

        var bytes = new byte[writer.Written];

        Assert.That(writer.TryWrite(bytes), Is.True);
        Assert.That(span.SequenceEqual(bytes), Is.True);

        bytes.AsSpan().Clear();
        Assert.That(span.SequenceEqual(bytes), Is.False);

        var fixedBuffer = new ValueFixedBufferWriter<byte>(bytes);

        Assert.That(fixedBuffer.Capacity, Is.EqualTo(bytes.Length));
        Assert.That(fixedBuffer.FreeCapacity, Is.EqualTo(bytes.Length));
        Assert.That(fixedBuffer.Written, Is.EqualTo(0));

        writer.Write(ref fixedBuffer);

        Assert.That(fixedBuffer.Written, Is.EqualTo(bytes.Length));

        Assert.That(span.SequenceEqual(bytes), Is.True);
    }
}