namespace IT.Buffers.Tests;

public class ReadOnlySequenceBuilderCharTest : ReadOnlySequenceBuilderTest<char>
{
    private ReadOnlyMemory<char>[] Data = [
        "1 segment".ToCharArray(),
        "2 segment".ToCharArray(),
        "3 segment".ToCharArray()
        ];

    protected override IEnumerable<ReadOnlyMemory<char>> GetMemories() => Data;

    protected override void ReadMemory(ReadOnlyMemory<char> memory, int index)
    {
        Assert.That(Data[index].Span.SequenceEqual(memory.Span), Is.True);
    }
}