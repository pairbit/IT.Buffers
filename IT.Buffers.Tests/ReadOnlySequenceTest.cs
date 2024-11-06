using System.Buffers;

namespace IT.Buffers.Tests;

public abstract class ReadOnlySequenceTest<T>
{
    [Test]
    public void Test()
    {
        var builder = ReadOnlySequenceBuilderPool<T>.Rent();
        
		try
		{
            Builder(builder);
        }
		finally
		{
            ReadOnlySequenceBuilderPool<T>.Return(builder);
        }
    }

    private void Builder(ReadOnlySequenceBuilder<T> builder)
    {
        foreach (var memory in GetMemories())
        {
            builder.Add(memory);
        }

        Sequence(builder.Build());
    }

    private void Sequence(ReadOnlySequence<T> sequence)
    {
        var position = sequence.Start;
        while (sequence.TryGet(ref position, out var memory))
        {
            ReadMemory(memory, position);

            if (position.GetObject() == null) break;
        }
    }

    protected abstract IEnumerable<ReadOnlyMemory<T>> GetMemories();

    protected abstract void ReadMemory(ReadOnlyMemory<T> memory, SequencePosition position);
}