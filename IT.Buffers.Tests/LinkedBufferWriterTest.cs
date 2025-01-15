namespace IT.Buffers.Tests;

public class LinkedBufferWriterTest
{
    [Test]
    public void Test_Pool()
    {
        var writer = LinkedBufferWriter<byte>.Pool.Rent();
        Assert.That(writer.Segments, Is.EqualTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetWrittenMemory(0));
        try
        {
            Test(writer);
        }
        finally
        {
            LinkedBufferWriter<byte>.Pool.Return(writer);
        }
    }

    [Test]
    public void Test_FirstBuffer()
    {
        var writer = new LinkedBufferWriter<byte>(BufferSize.KB, true);

        Assert.That(writer.Segments, Is.EqualTo(1));
        Assert.That(writer.GetWrittenMemory(0).IsEmpty, Is.True);

        Test(writer);
    }

    private void Test(LinkedBufferWriter<byte> writer)
    {
        var i = 0;
        var e = writer.GetEnumerator();
        Assert.That(e.MoveNext(), Is.False);

        for (int s = 0; s < 5; s++)
        {
            var span = writer.GetSpan();
            if (span.IsEmpty) span = writer.GetSpan(1);

            Assert.That(writer.Segments, Is.EqualTo(s + 1));

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

        writer.Reset();

        for (int s = 0; s < 5; s++)
        {
            var span = writer.GetSpan();
            if (span.IsEmpty) span = writer.GetSpan(1);

            Assert.That(writer.Segments, Is.EqualTo(s + 1));

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
}