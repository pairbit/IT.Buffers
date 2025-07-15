using IT.Buffers.Tests.Internal.Iteration;

namespace IT.Buffers.Tests.Iteration;

internal class ReadOnlySequenceIteratorTest : ReadOnlySpanIteratorTest<ReadOnlySequenceIterator<byte>, byte>
{
    private readonly ReadOnlyMemory<byte>[] Segments = [
        "1 segment"u8.ToArray(),
        "2 segment"u8.ToArray(),
        "3 segment"u8.ToArray()
        ];

    protected override ReadOnlySequenceIterator<byte> GetIterator()
    {
        var builder = new ReadOnlySequenceBuilder<byte>(Segments.Length);

        foreach (var memory in Segments)
        {
            builder.Add(memory);
        }

        return new ReadOnlySequenceIterator<byte>(builder.Build());
    }

    protected override void Iterate(ReadOnlySpan<byte> span, int index)
    {
        Assert.That(Segments[index].Span.SequenceEqual(span), Is.True);
    }
}