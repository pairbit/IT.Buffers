namespace IT.Buffers.Tests.Iteration;

internal class MemoriesIteratorTest : ReadOnlySpanIteratorTest<MemoriesIterator<byte>, byte>
{
    private readonly Memory<byte>[] Segments = [
        "1 segmen"u8.ToArray(),
        "2 segment"u8.ToArray(),
        "3 segment"u8.ToArray()
        ];

    protected override MemoriesIterator<byte> GetIterator()
    {
        return new MemoriesIterator<byte>(Segments);
    }

    protected override void Iterate(ReadOnlySpan<byte> span, int index)
    {
        Assert.That(Segments[index].Span.SequenceEqual(span), Is.True);
    }
}