
namespace IT.Buffers.Tests;

public class ReadOnlySequenceCharTest : ReadOnlySequenceTest<char>
{
    protected override IEnumerable<ReadOnlyMemory<char>> GetMemories()
    {
        yield return "1 segment".ToCharArray();
        yield return "2 segment".ToCharArray();
        yield return "3 segment".ToCharArray();
    }

    protected override void ReadMemory(ReadOnlyMemory<char> memory, SequencePosition position)
    {

    }
}