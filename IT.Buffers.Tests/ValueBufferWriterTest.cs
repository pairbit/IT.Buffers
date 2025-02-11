using IT.Buffers.Interfaces;

namespace IT.Buffers.Tests;

public class ValueBufferWriterTest
{
    [Test]
    public void Test_Rented()
    {
        ValueRentedBufferWriter<byte> writer = default;

        try
        {
            Test(ref writer);
            Assert.That(writer.Written, Is.Not.EqualTo(0));
        }
        finally
        {
            writer.Reset();
        }
    }

    [Test]
    public void Test_Fixed()
    {
        ValueFixedMemoryBufferWriter<byte> memoryBuffer = default;

        Assert.That(memoryBuffer.Capacity, Is.EqualTo(0));
        Assert.That(memoryBuffer.FreeCapacity, Is.EqualTo(0));
        Assert.That(memoryBuffer.Written, Is.EqualTo(0));
        Assert.That(memoryBuffer.GetSpan().IsEmpty, Is.True);
        Assert.That(memoryBuffer.GetMemory().IsEmpty, Is.True);

        ValueFixedArrayBufferWriter<byte> arrayBuffer = default;

        Assert.That(arrayBuffer.Capacity, Is.EqualTo(0));
        Assert.That(arrayBuffer.FreeCapacity, Is.EqualTo(0));
        Assert.That(arrayBuffer.Written, Is.EqualTo(0));
        Assert.That(arrayBuffer.GetSpan().IsEmpty, Is.True);
        Assert.That(arrayBuffer.GetMemory().IsEmpty, Is.True);
    }

    private static void Test<TBufferWriter>(ref TBufferWriter writer)
        where TBufferWriter : IAdvancedBufferWriter<byte>
    {
        Assert.That(writer.Written, Is.EqualTo(0));

        var span = writer.GetSpan(1);

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        Assert.That(writer.Written, Is.EqualTo(span.Length));
        Assert.That(writer.GetSpan().IsEmpty, Is.True);
        Assert.That(writer.GetMemory().IsEmpty, Is.True);

        var bytes = new byte[writer.Written + 10];
        var memory = bytes.AsMemory(5, writer.Written);

        Assert.That(writer.TryWrite(memory.Span), Is.True);
        Assert.That(span.SequenceEqual(memory.Span), Is.True);

        memory.Span.Clear();
        Assert.That(span.SequenceEqual(memory.Span), Is.False);

        var memoryBuffer = new ValueFixedMemoryBufferWriter<byte>(memory);

        Assert.That(memoryBuffer.Capacity, Is.EqualTo(memory.Length));
        Assert.That(memoryBuffer.FreeCapacity, Is.EqualTo(memory.Length));
        Assert.That(memoryBuffer.Written, Is.EqualTo(0));

        writer.Write(ref memoryBuffer);

        Assert.That(memoryBuffer.Written, Is.EqualTo(memory.Length));
        Assert.That(memoryBuffer.GetSpan().IsEmpty, Is.True);
        Assert.That(memoryBuffer.GetMemory().IsEmpty, Is.True);

        Assert.That(span.SequenceEqual(memory.Span), Is.True);

        bytes = new byte[writer.Written];

        Assert.That(writer.TryWrite(bytes), Is.True);
        Assert.That(span.SequenceEqual(bytes), Is.True);

        bytes.AsSpan().Clear();
        Assert.That(span.SequenceEqual(bytes), Is.False);

        var arrayBuffer = new ValueFixedArrayBufferWriter<byte>(bytes);

        Assert.That(arrayBuffer.Capacity, Is.EqualTo(bytes.Length));
        Assert.That(arrayBuffer.FreeCapacity, Is.EqualTo(bytes.Length));
        Assert.That(arrayBuffer.Written, Is.EqualTo(0));

        writer.Write(ref arrayBuffer);

        Assert.That(arrayBuffer.Written, Is.EqualTo(bytes.Length));
        Assert.That(arrayBuffer.GetSpan().IsEmpty, Is.True);
        Assert.That(arrayBuffer.GetMemory().IsEmpty, Is.True);

        Assert.That(span.SequenceEqual(bytes), Is.True);
    }
}