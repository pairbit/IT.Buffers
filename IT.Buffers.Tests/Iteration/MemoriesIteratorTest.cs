namespace IT.Buffers.Tests.Iteration;

internal class MemoriesIteratorTest : ReadOnlySpanIteratorTest<MemoriesIterator<byte>, byte>
{
    private readonly Memory<byte>[] Segments = [
        "start --[my"u8.ToArray(),
        " best "u8.ToArray(),
        "content]-- end"u8.ToArray()
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