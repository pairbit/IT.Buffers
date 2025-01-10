﻿namespace IT.Buffers.Tests;

public class RentedBufferWriterTest
{
    [Test]
    public void Test()
    {
        using var buffer = new RentedBufferWriter<byte>();
        Test(buffer);
    }

    private void Test(RentedBufferWriter<byte> bufferWriter)
    {
        var capacity = RentedBufferWriter<byte>.MinimumBufferSize;

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

        
    }
}