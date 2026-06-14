using IT.Buffers.Extensions;
using System.Buffers;

namespace IT.Buffers.Tests;

public class BufferWriterTest
{
    [Test]
    public void Test_GetSpanGetSpan()
    {
        var writer = new BufferWriter<byte>();

        var span = writer.GetSpan();
        var span2 = writer.GetSpan();
        var span3 = writer.GetSpan(32);
    }

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
    public void Test_WriteToAndReset()
    {
        var writer = new BufferWriter<byte>();
        var writer2 = new BufferWriter<byte>();

        writer.WriteToAndReset(ref writer2);

        var span = writer.GetSpan();
        Assert.That(writer.Segments, Is.EqualTo(1));

        writer.WriteToAndReset(ref writer2);

        Assert.That(writer.Segments, Is.EqualTo(0));
    }

    [Test]
    public async Task Test_WriteToAndResetAsync()
    {
        var writer = new BufferWriter<byte>();
        var stream = new BufferWriterStream(new BufferWriter<byte>());

        await writer.WriteToAndResetAsync(stream);

        var span = writer.GetSpan();
        Assert.That(writer.Segments, Is.EqualTo(1));

        await writer.WriteToAndResetAsync(stream);

        Assert.That(writer.Segments, Is.EqualTo(0));
    }

    [Test]
    public async Task Test_WriteAsync()
    {
        var writer = new BufferWriter<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            await writer.WriteAsync(stream);

            Assert.That(writer.Written, Is.EqualTo(bytes.Length));
            Assert.That(writer.Segments, Is.EqualTo(17));
        }
        finally
        {
            writer.Reset();
        }
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