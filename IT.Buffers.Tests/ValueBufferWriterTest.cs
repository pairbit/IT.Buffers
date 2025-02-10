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
        ValueFixedBufferWriter<byte> writer = default;

        Assert.That(writer.Capacity, Is.EqualTo(0));
        Assert.That(writer.FreeCapacity, Is.EqualTo(0));
        Assert.That(writer.Written, Is.EqualTo(0));
        Assert.That(writer.GetSpan().IsEmpty, Is.True);
        Assert.That(writer.GetMemory().IsEmpty, Is.True);
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

        var fixedBuffer = new ValueFixedBufferWriter<byte>(memory);

        Assert.That(fixedBuffer.Capacity, Is.EqualTo(memory.Length));
        Assert.That(fixedBuffer.FreeCapacity, Is.EqualTo(memory.Length));
        Assert.That(fixedBuffer.Written, Is.EqualTo(0));

        writer.Write(ref fixedBuffer);

        Assert.That(fixedBuffer.Written, Is.EqualTo(memory.Length));
        Assert.That(fixedBuffer.GetSpan().IsEmpty, Is.True);
        Assert.That(fixedBuffer.GetMemory().IsEmpty, Is.True);

        Assert.That(span.SequenceEqual(memory.Span), Is.True);
    }
}