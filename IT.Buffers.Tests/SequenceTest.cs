using IT.Buffers.Extensions;
using System.Buffers;

namespace IT.Buffers.Tests;

internal class SequenceTest
{
    [Test]
    public void Test_GetSpanGetSpan()
    {
        var sequence = new Sequence<byte>();

        var span = sequence.GetSpan();
        var span2 = sequence.GetSpan();
        var span3 = sequence.GetSpan(span.Length + 1);
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

            var ros = sequence.AsReadOnlySequence;
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

            ros = sequence.AsReadOnlySequence;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            var lastROS = ros.Slice(start);
            Assert.That(lastROS.Length, Is.EqualTo(lastBuffer.Length));
            Assert.That(lastROS.SequenceEqual(lastBuffer), Is.True);

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length + lastBuffer.Length));
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

            sequence.ArrayPool = GrowingArrayPool<byte>.TwoOfEachSize;

            sequence.NextBufferSize = BufferSize.KB_64;

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnlySequence;
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

            sequence.ArrayPool = GrowingArrayPool<byte>.FourOfEachSize;
            sequence.NextBufferSize = BufferSize.KB;

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnlySequence;
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