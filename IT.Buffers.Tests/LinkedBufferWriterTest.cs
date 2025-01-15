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
        var e = writer.GetEnumerator();
        Assert.That(e.MoveNext(), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetWrittenMemory(0));

        var span = writer.GetSpan();

        Assert.That(writer.Segments, Is.EqualTo(1));

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        i = 0;
        e.Reset();
        while (e.MoveNext())
        {
            Assert.That(e.Current.Span.SequenceEqual(writer.GetWrittenMemory(i++).Span), Is.True);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetWrittenMemory(i));

        span = writer.GetSpan();

        Assert.That(writer.Segments, Is.EqualTo(2));

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        i = 0;
        e.Reset();
        while (e.MoveNext())
        {
            Assert.That(e.Current.Span.SequenceEqual(writer.GetWrittenMemory(i++).Span), Is.True);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetWrittenMemory(i));
    }
}