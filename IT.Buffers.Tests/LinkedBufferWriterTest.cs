using System;

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
        Assert.That(writer.Segments, Is.EqualTo(0));
        var i = 0;
        foreach (Memory<byte> memory in writer)
        {
            Assert.That(memory.Span.SequenceEqual(writer.GetWrittenMemory(i++).Span), Is.True);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetWrittenMemory(0));

        var span = writer.GetSpan();

        Assert.That(writer.Segments, Is.EqualTo(1));

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        i = 0;
        foreach (Memory<byte> memory in writer)
        {
            Assert.That(memory.Span.SequenceEqual(writer.GetWrittenMemory(i++).Span), Is.True);
        }

        span = writer.GetSpan();

        Assert.That(writer.Segments, Is.EqualTo(2));

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        i = 0;
        foreach (Memory<byte> memory in writer)
        {
            Assert.That(memory.Span.SequenceEqual(writer.GetWrittenMemory(i++).Span), Is.True);
        }
    }
}