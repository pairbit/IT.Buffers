namespace IT.Buffers.Tests;

public class ReadOnlySequenceBuilderByteTest : ReadOnlySequenceBuilderTest<byte>
{
    private ReadOnlyMemory<byte>[] Data = [
        "1 segment"u8.ToArray(),
        "2 segment"u8.ToArray(),
        "3 segment"u8.ToArray()
        ];

    protected override IEnumerable<ReadOnlyMemory<byte>> GetMemories() => Data;

    protected override void ReadMemory(ReadOnlyMemory<byte> memory, int index)
    {
        Assert.That(Data[index].Span.SequenceEqual(memory.Span), Is.True);
    }
}