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
    public void Test_Default()
    {
        ValueFixedMemoryBufferWriter<byte> memoryBuffer = default;

        Assert.That(memoryBuffer.Capacity, Is.EqualTo(0));
        Assert.That(memoryBuffer.FreeCapacity, Is.EqualTo(0));
        Assert.That(memoryBuffer.Written, Is.EqualTo(0));
        Assert.That(memoryBuffer.GetSpan().IsEmpty, Is.True);
        Assert.That(memoryBuffer.GetMemory().IsEmpty, Is.True);
        Assert.Throws<OutOfMemoryException>(() => memoryBuffer.GetSpan(1));
        Assert.Throws<OutOfMemoryException>(() => memoryBuffer.GetMemory(1));

        ValueFixedArrayBufferWriter<byte> arrayBuffer = default;

        Assert.That(arrayBuffer.Capacity, Is.EqualTo(0));
        Assert.That(arrayBuffer.FreeCapacity, Is.EqualTo(0));
        Assert.That(arrayBuffer.Written, Is.EqualTo(0));
        Assert.That(arrayBuffer.GetSpan().IsEmpty, Is.True);
        Assert.That(arrayBuffer.GetMemory().IsEmpty, Is.True);
        Assert.Throws<OutOfMemoryException>(() => arrayBuffer.GetSpan(1));
        Assert.Throws<OutOfMemoryException>(() => arrayBuffer.GetMemory(1));

        ValueFixedSpanBufferWriter<byte> spanBuffer = default;

        Assert.That(spanBuffer.Capacity, Is.EqualTo(0));
        Assert.That(spanBuffer.FreeCapacity, Is.EqualTo(0));
        Assert.That(spanBuffer.Written, Is.EqualTo(0));
        Assert.That(spanBuffer.GetSpan().IsEmpty, Is.True);
        try
        {
            spanBuffer.GetSpan(1);
            Assert.Fail();
        }
        catch (OutOfMemoryException ex)
        {
            Assert.That(ex.Message, Is.EqualTo("SizeHint 1 > 0"));
        }
    }

    private static void Test<TBufferWriter>(ref TBufferWriter writer)
        where TBufferWriter : IAdvancedBufferWriter<byte>
    {
        Assert.That(writer.Written, Is.EqualTo(0));

        var span = writer.GetSpan();
        Assert.That(span.Length > 0, Is.True);

        Random.Shared.NextBytes(span);

        writer.Advance(span.Length);

        Assert.That(writer.Written, Is.EqualTo(span.Length));
        
        if (!writer.HasMemory)
        {
            try
            {
                writer.GetMemory();
                Assert.Fail();
            }
            catch (NotSupportedException ex)
            {
                Assert.That(ex.Message, Is.EqualTo("Method 'GetMemory' is not supported"));
            }
        }

        TestArray(ref writer, span);

        TestMemory(ref writer, span);

        TestSpan(ref writer, span);
    }

    private static void TestSpan<TBufferWriter>(ref TBufferWriter writer, Span<byte> data)
        where TBufferWriter : IAdvancedBufferWriter<byte>
    {
        Assert.That(writer.Written, Is.LessThanOrEqualTo(255));

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

#if NET9_0_OR_GREATER
        writer.Write(ref spanBuffer);
#else
        writer.TryWrite(spanBuffer.GetSpan(writer.Written));
        spanBuffer.Advance(writer.Written);
#endif
        Assert.That(spanBuffer.Written, Is.EqualTo(span.Length));
        Assert.That(spanBuffer.GetSpan().IsEmpty, Is.True);

        Assert.That(data.SequenceEqual(span), Is.True);
    }

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