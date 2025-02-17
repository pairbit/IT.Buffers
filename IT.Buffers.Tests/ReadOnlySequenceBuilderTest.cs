﻿using System.Buffers;

namespace IT.Buffers.Tests;

public abstract class ReadOnlySequenceBuilderTest<T>
{
    [Test]
    public void Test()
    {
        var builder = ReadOnlySequenceBuilder<T>.Pool.Rent();
        
		try
		{
            Builder(builder);
        }
		finally
		{
            Assert.That(ReadOnlySequenceBuilder<T>.Pool.TryReturn(builder));
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

    private void Sequence(in ReadOnlySequence<T> sequence)
    {
        var position = sequence.Start;

        var i = 0;

        while (sequence.TryGet(ref position, out var memory))
        {
            ReadMemory(memory, i++);

            if (position.GetObject() == null) break;
        }

        Assert.That(BufferPool.TryReturn(sequence), Is.EqualTo(0));
    }

    protected abstract IEnumerable<ReadOnlyMemory<T>> GetMemories();

    protected abstract void ReadMemory(ReadOnlyMemory<T> memory, int index);
}