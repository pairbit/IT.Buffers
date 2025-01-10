using IT.Buffers.Pool;

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
        Assert.That(bufferWriter.WrittenCount, Is.EqualTo(0));
        Assert.That(bufferWriter.WrittenMemory.Length, Is.EqualTo(0));

        var span = bufferWriter.GetSpan();

        Assert.That(span.Length, Is.EqualTo(capacity));

        Random.Shared.NextBytes(span);
        bufferWriter.Advance(span.Length);

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.FreeCapacity, Is.EqualTo(0));
        Assert.That(bufferWriter.WrittenCount, Is.EqualTo(span.Length));
        Assert.That(bufferWriter.WrittenMemory.Length, Is.EqualTo(span.Length));

        span = bufferWriter.GetSpan();

        capacity *= 2;

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.FreeCapacity, Is.EqualTo(capacity - bufferWriter.WrittenCount));
        Assert.That(span.Length, Is.EqualTo(capacity - bufferWriter.WrittenCount));

        Random.Shared.NextBytes(span);
        bufferWriter.Advance(span.Length);

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.WrittenCount, Is.EqualTo(capacity));

        bufferWriter.ResetWrittenCount();

        Assert.That(bufferWriter.Capacity, Is.EqualTo(capacity));
        Assert.That(bufferWriter.WrittenCount, Is.EqualTo(0));

        var newSpan = bufferWriter.GetSpan();

        Assert.That(newSpan.Slice(span.Length).SequenceEqual(span), Is.True);

        bufferWriter.Clear();

        Assert.That(newSpan.Slice(span.Length).SequenceEqual(span), Is.True);

        newSpan = bufferWriter.GetSpan(bufferWriter.Capacity + 1);

        Assert.That(newSpan.Slice(span.Length).SequenceEqual(span), Is.False);

        Assert.Throws<InvalidOperationException>(() => bufferWriter.Initialize(2));

        bufferWriter.Return();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            var c = bufferWriter.Capacity;
        });

        bufferWriter.Initialize(3);

        Assert.That(bufferWriter.Capacity, Is.EqualTo(16));

        bufferWriter.Return();
    }
}