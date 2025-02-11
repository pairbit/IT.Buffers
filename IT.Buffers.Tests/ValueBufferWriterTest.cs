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

        TestArray(ref writer, span);

        TestMemory(ref writer, span);

#if NET9_0_OR_GREATER
        TestSpan(ref writer, span);
#endif
    }

#if NET9_0_OR_GREATER
    private static void TestSpan<TBufferWriter>(ref TBufferWriter writer, Span<byte> data)
        where TBufferWriter : IAdvancedBufferWriter<byte>
    {
        Span<byte> bytes = stackalloc byte[writer.Written + 10];
        var span = bytes.Slice(5, writer.Written);

        Assert.That(writer.TryWrite(span), Is.True);
        Assert.That(data.SequenceEqual(span), Is.True);

        span.Clear();
        Assert.That(data.SequenceEqual(span), Is.False);

        var spanBuffer = new ValueFixedSpanBufferWriter<byte>(span);

        Assert.That(spanBuffer.Capacity, Is.EqualTo(span.Length));
        Assert.That(spanBuffer.FreeCapacity, Is.EqualTo(span.Length));
        Assert.That(spanBuffer.Written, Is.EqualTo(0));

        writer.Write(ref spanBuffer);

        Assert.That(spanBuffer.Written, Is.EqualTo(span.Length));
        Assert.That(spanBuffer.GetSpan().IsEmpty, Is.True);
        //Assert.That(spanBuffer.GetMemory().IsEmpty, Is.True);

        Assert.That(data.SequenceEqual(span), Is.True);
    }
#endif

    private static void TestMemory<TBufferWriter>(ref TBufferWriter writer, Span<byte> data)
        where TBufferWriter : IAdvancedBufferWriter<byte>
    {
        var array = new byte[writer.Written + 10];
        var memory = array.AsMemory(5, writer.Written);

        Assert.That(writer.TryWrite(memory.Span), Is.True);
        Assert.That(data.SequenceEqual(memory.Span), Is.True);

        memory.Span.Clear();
        Assert.That(data.SequenceEqual(memory.Span), Is.False);

        var memoryBuffer = new ValueFixedMemoryBufferWriter<byte>(memory);

        Assert.That(memoryBuffer.Capacity, Is.EqualTo(memory.Length));
        Assert.That(memoryBuffer.FreeCapacity, Is.EqualTo(memory.Length));
        Assert.That(memoryBuffer.Written, Is.EqualTo(0));

        writer.Write(ref memoryBuffer);

        Assert.That(memoryBuffer.Written, Is.EqualTo(memory.Length));
        Assert.That(memoryBuffer.GetSpan().IsEmpty, Is.True);
        Assert.That(memoryBuffer.GetMemory().IsEmpty, Is.True);

        Assert.That(data.SequenceEqual(memory.Span), Is.True);
    }

    private static void TestArray<TBufferWriter>(ref TBufferWriter writer, Span<byte> data)
        where TBufferWriter : IAdvancedBufferWriter<byte>
    {
        var array = new byte[writer.Written];

        Assert.That(writer.TryWrite(array), Is.True);
        Assert.That(data.SequenceEqual(array), Is.True);

        array.AsSpan().Clear();
        Assert.That(data.SequenceEqual(array), Is.False);

        var arrayBuffer = new ValueFixedArrayBufferWriter<byte>(array);

        Assert.That(arrayBuffer.Capacity, Is.EqualTo(array.Length));
        Assert.That(arrayBuffer.FreeCapacity, Is.EqualTo(array.Length));
        Assert.That(arrayBuffer.Written, Is.EqualTo(0));

        writer.Write(ref arrayBuffer);

        Assert.That(arrayBuffer.Written, Is.EqualTo(array.Length));
        Assert.That(arrayBuffer.GetSpan().IsEmpty, Is.True);
        Assert.That(arrayBuffer.GetMemory().IsEmpty, Is.True);

        Assert.That(data.SequenceEqual(array), Is.True);
    }
}