using IT.Buffers.Extensions;

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
    public async Task Test_WriteAsync()
    {
        var sequence = new Sequence<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            //sequence.NextBufferSize = BufferSize.KB_64;
            //sequence.GetSpan(BufferSize.KB_128);

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnlySequence;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            //Assert.That(sequence.Segments, Is.EqualTo(5));
            //Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.MB));
        }
        finally
        {
            sequence.Reset();
        }
    }
}