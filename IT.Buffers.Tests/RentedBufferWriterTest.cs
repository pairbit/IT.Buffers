namespace IT.Buffers.Tests;

public class RentedBufferWriterTest
{
    [Test]
    public void Test()
    {
        var buffer = RentedBufferWriterPool<byte>.Rent();

        Test(buffer);

        RentedBufferWriterPool<byte>.Return(buffer);
    }

    private void Test(RentedBufferWriter<byte> bufferWriter)
    {
        var capacity = BufferSize.Min;

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.FreeCapacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.Written, Is.EqualTo(0));
        Assert.That(bufferWriter.WrittenMemory.Length, Is.EqualTo(0));

        var span = bufferWriter.GetSpan();

        Assert.That(span.Length, Is.EqualTo(capacity));

        Random.Shared.NextBytes(span);
        bufferWriter.Advance(span.Length);

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.FreeCapacity, Is.EqualTo(0));
        Assert.That(bufferWriter.Written, Is.EqualTo(span.Length));
        Assert.That(bufferWriter.WrittenMemory.Length, Is.EqualTo(span.Length));

        span = bufferWriter.GetSpan();

        capacity *= 2;

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.FreeCapacity, Is.EqualTo(capacity - bufferWriter.Written));
        Assert.That(span.Length, Is.EqualTo(capacity - bufferWriter.Written));

        Random.Shared.NextBytes(span);
        bufferWriter.Advance(span.Length);

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.Written, Is.EqualTo(capacity));

        bufferWriter.ResetWritten();

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.Written, Is.EqualTo(0));

        var newSpan = bufferWriter.GetSpan();

        Assert.That(newSpan.Slice(span.Length).SequenceEqual(span), Is.True);

        newSpan = bufferWriter.GetSpan(bufferWriter.Capacity + 1);

        Assert.That(newSpan.Slice(span.Length).SequenceEqual(span), Is.False);

        bufferWriter.Dispose();

        Assert.That(bufferWriter.Capacity, Is.EqualTo(0));

        newSpan = bufferWriter.GetSpan();

        Assert.That(bufferWriter.Capacity, Is.EqualTo(16));
    }
}