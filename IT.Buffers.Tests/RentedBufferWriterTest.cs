namespace IT.Buffers.Tests;

public class RentedBufferWriterTest
{
    [Test]
    public void Test()
    {
        var buffer = RentedBufferWriter<byte>.Pool.Rent();

        try
        {
            Test(buffer);
        }
        finally
        {
            Assert.That(RentedBufferWriter<byte>.Pool.TryReturn(buffer), Is.True);
        }
    }

    private void Test(RentedBufferWriter<byte> writer)
    {
        Assert.That(writer.Capacity, Is.EqualTo(0));
        Assert.That(writer.FreeCapacity, Is.EqualTo(0));
        Assert.That(writer.Written, Is.EqualTo(0));
        Assert.That(writer.WrittenMemory.Length, Is.EqualTo(0));

        var span = writer.GetSpan();

        var capacity = 16;
        Assert.That(span.Length, Is.EqualTo(capacity));

        Random.Shared.NextBytes(span);
        writer.Advance(span.Length);

        Assert.That(writer.Capacity, Is.EqualTo(capacity));
        Assert.That(writer.FreeCapacity, Is.EqualTo(0));
        Assert.That(writer.Written, Is.EqualTo(span.Length));
        Assert.That(writer.WrittenMemory.Length, Is.EqualTo(span.Length));

        span = writer.GetSpan();

        capacity *= 2;
        
        Assert.That(span.Length, Is.EqualTo(capacity - writer.Written));
        Assert.That(writer.Capacity, Is.EqualTo(capacity));
        Assert.That(writer.FreeCapacity, Is.EqualTo(capacity - writer.Written));

        Random.Shared.NextBytes(span);
        writer.Advance(span.Length);

        Assert.That(writer.Capacity, Is.EqualTo(capacity));
        Assert.That(writer.Written, Is.EqualTo(capacity));

        writer.ResetWritten();

        Assert.That(writer.Capacity, Is.EqualTo(capacity));
        Assert.That(writer.Written, Is.EqualTo(0));

        var newSpan = writer.GetSpan();

        Assert.That(newSpan.Slice(span.Length).SequenceEqual(span), Is.True);

        newSpan = writer.GetSpan(writer.Capacity + 1);

        Assert.That(newSpan.Slice(span.Length).SequenceEqual(span), Is.False);

        writer.Reset();

        Assert.That(writer.Capacity, Is.EqualTo(0));

        newSpan = writer.GetSpan();

        Assert.That(writer.Capacity, Is.EqualTo(16));
    }
}