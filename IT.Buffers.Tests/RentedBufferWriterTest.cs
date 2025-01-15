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
            RentedBufferWriter<byte>.Pool.Return(buffer);
        }
    }

    private void Test(RentedBufferWriter<byte> bufferWriter)
    {
        Assert.That(bufferWriter.Capacity, Is.EqualTo(0));
        Assert.That(bufferWriter.FreeCapacity, Is.EqualTo(0));
        Assert.That(bufferWriter.Written, Is.EqualTo(0));
        Assert.That(bufferWriter.WrittenMemory.Length, Is.EqualTo(0));

        var span = bufferWriter.GetSpan(1);

        var capacity = 16;
        Assert.That(span.Length, Is.EqualTo(capacity));

        Random.Shared.NextBytes(span);
        bufferWriter.Advance(span.Length);

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.FreeCapacity, Is.EqualTo(0));
        Assert.That(bufferWriter.Written, Is.EqualTo(span.Length));
        Assert.That(bufferWriter.WrittenMemory.Length, Is.EqualTo(span.Length));
        Assert.That(bufferWriter.GetSpan().IsEmpty, Is.True);
        Assert.That(bufferWriter.GetMemory().IsEmpty, Is.True);

        span = bufferWriter.GetSpan(1);

        capacity *= 2;
        
        Assert.That(span.Length, Is.EqualTo(capacity - bufferWriter.Written));
        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.FreeCapacity, Is.EqualTo(capacity - bufferWriter.Written));

        Random.Shared.NextBytes(span);
        bufferWriter.Advance(span.Length);

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.Written, Is.EqualTo(capacity));
        Assert.That(bufferWriter.GetSpan().IsEmpty, Is.True);
        Assert.That(bufferWriter.GetMemory().IsEmpty, Is.True);

        bufferWriter.ResetWritten();

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.Written, Is.EqualTo(0));

        var newSpan = bufferWriter.GetSpan();

        Assert.That(newSpan.Slice(span.Length).SequenceEqual(span), Is.True);

        newSpan = bufferWriter.GetSpan(bufferWriter.Capacity + 1);

        Assert.That(newSpan.Slice(span.Length).SequenceEqual(span), Is.False);

        bufferWriter.Dispose();

        Assert.That(bufferWriter.Capacity, Is.EqualTo(0));

        newSpan = bufferWriter.GetSpan(1);

        Assert.That(bufferWriter.Capacity, Is.EqualTo(16));
    }
}