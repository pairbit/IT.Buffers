using IT.Buffers.Extensions;
using System.Buffers;

namespace IT.Buffers.Tests;

public class BufferWriterTest
{
    [Test]
    public void Test_ToArrayAndReset()
    {
        var writer = new BufferWriter<byte>();

        Assert.That(writer.ToArrayAndReset(), Is.Empty);

        var span = writer.GetSpan();
        Assert.That(writer.Segments, Is.EqualTo(1));

        Assert.That(writer.ToArrayAndReset(), Is.Empty);

        Assert.That(writer.Segments, Is.EqualTo(0));
    }

    [Test]
    public void Test_TryWriteToAndReset()
    {
        var writer = new BufferWriter<byte>();

        Assert.That(writer.TryWriteToAndReset(default), Is.True);

        var span = writer.GetSpan();
        Assert.That(writer.Segments, Is.EqualTo(1));

        Assert.That(writer.TryWriteToAndReset(default), Is.True);

        Assert.That(writer.Segments, Is.EqualTo(0));
    }

    [Test]
    public void Test_Pool()
    {
        var writer = BufferWriter<byte>.Pool.Rent();
        Assert.That(writer.Segments, Is.EqualTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetWrittenMemory(0));
        try
        {
            Test(writer);
        }
        finally
        {
            Assert.That(BufferWriter<byte>.Pool.TryReturn(writer), Is.True);
        }
    }

    [Test]
    public void Test_FirstBuffer()
    {
        var writer = new InitedBufferWriter<byte>(new byte[BufferSize.KB]);

        Assert.That(writer.Segments, Is.EqualTo(1));
        Assert.That(writer.GetWrittenMemory(0).IsEmpty, Is.True);

        //Test(writer);
    }

    private void Test(BufferWriter<byte> writer)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(-1));
        var i = 0;
        var e = writer.GetEnumerator();
        Assert.That(e.MoveNext(), Is.False);
        writer.ArrayPool = new LimitedSharedArrayPool<byte>(0);
        for (int s = 0; s < 5; s++)
        {
            var span = writer.GetSpan();

            Assert.Throws<InvalidOperationException>(() => writer.ArrayPool = null);

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
        writer.ArrayPool = new LimitedSharedArrayPool<byte>(64);

        for (int s = 0; s < 5; s++)
        {
            var span = writer.GetSpan();

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

        writer.Advance(0);
        writer.Write(ReadOnlySpan<byte>.Empty);
    }
}