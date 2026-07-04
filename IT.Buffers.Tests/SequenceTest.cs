using IT.Buffers.Extensions;
using System.Buffers;

namespace IT.Buffers.Tests;

internal class SequenceTest
{
    [Test]
    public void LeakTest()
    {
        var sequence = new Sequence<object>();
        var span = sequence.GetSpan(BufferSize.KB);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = new object();
        }
        sequence.Reset();
        for (int i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.Null);
        }
    }

    [Test]
    public void Test_GetSpanGetSpan()
    {
        var sequence = new Sequence<byte>();

        var span = sequence.GetSpan();
        var span2 = sequence.GetSpan();
        var span3 = sequence.GetSpan(span.Length + 1);
    }

    [Test]
    public void Advance_Test()
    {
        var sequence = new Sequence<byte>();

        Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Advance(1));

        var span = sequence.GetSpan();
        sequence.Advance(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Advance(int.MaxValue));
    }

    [Test]
    public async Task WriteStream_OneOfEachSize_Test()
    {
        var sequence = new Sequence<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            sequence.NextBufferSize = BufferSize.KB_64;
            sequence.GetSpan(BufferSize.KB_128);

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnly;
            var start = sequence.End;

            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            Assert.That(ros.SequenceEqual(bytes), Is.True);
            Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.MB));

            sequence.NextBufferSize = BufferSize.KB;
            var lastBuffer = new byte[BufferSize.KB_80];
            Random.Shared.NextBytes(lastBuffer);
            sequence.Write(lastBuffer);

            ros = sequence.AsReadOnly;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length + lastBuffer.Length));
            Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.KB_32));

            var lastROS = ros.Slice(start);
            Assert.That(lastROS.Length, Is.EqualTo(lastBuffer.Length));
            Assert.That(lastROS.SequenceEqual(lastBuffer), Is.True);

            sequence.AdvanceTo(start);
            ros = sequence.AsReadOnly;
            Assert.That(ros.SequenceEqual(lastROS), Is.True);
            Assert.That(ros.Length, Is.EqualTo(lastBuffer.Length));
            Assert.That(ros.SequenceEqual(lastBuffer), Is.True);

            Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.KB_32));
        }
        finally
        {
            sequence.Reset();
        }
    }

    [Test]
    public async Task WriteStream_TwoOfEachSize_Test()
    {
        var sequence = new Sequence<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            sequence.GrowthStrategy = BufferGrowthStrategy.TwoOfEachSize;
            sequence.NextBufferSize = BufferSize.KB_64;

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnly;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            Assert.That(sequence.NextBufferSize, Is.LessThanOrEqualTo(BufferSize.KB_512));
        }
        finally
        {
            sequence.Reset();
        }
    }

    [Test]
    public async Task WriteStream_FourOfEachSize_Test()
    {
        var sequence = new Sequence<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            sequence.GrowthStrategy = BufferGrowthStrategy.FourOfEachSize;
            sequence.NextBufferSize = BufferSize.KB;

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnly;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            Assert.That(sequence.NextBufferSize, Is.LessThanOrEqualTo(BufferSize.KB_256));
        }
        finally
        {
            sequence.Reset();
        }
    }
}