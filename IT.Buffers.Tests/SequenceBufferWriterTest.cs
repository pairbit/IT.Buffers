using IT.Buffers.Extensions;
using IT.Buffers.Interfaces;
using System.Buffers;

namespace IT.Buffers.Tests;

internal class SequenceBufferWriterTest
{
    [Test]
    public async Task New_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();
        try
        {
            var rentedBuffer = (IRentedBuffer<SequenceBufferWriter<byte>>)bufferWriter;
            Assert.That(rentedBuffer.BufferPool, Is.Null);

            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);

            bufferWriter.GetSpan(BufferSize.KB_8);
            bufferWriter.Write(bytes);

            var pos = bufferWriter.End;
            var ros = bufferWriter.AsReadOnly;

            using var ross = new ReadOnlySequenceStream(ros);

            await bufferWriter.WriteAsync(ross);

            var ros2 = bufferWriter.AsReadOnly;
            var sliced = ros2.Slice(pos);

            Assert.That(sliced.SequenceEqual(ros), Is.True);
        }
        finally
        {
            bufferWriter.Reset();
        }
    }

    [Test]
    public async Task Pool_Test()
    {
        var pool = SequenceBufferWriter<byte>.Pool;
        
        var bufferWriter = pool.Rent();
        try
        {
            var rentedBuffer = (IRentedBuffer<SequenceBufferWriter<byte>>)bufferWriter;
            Assert.That(rentedBuffer.BufferPool, Is.EqualTo(pool));
            Assert.That(rentedBuffer.BufferPool.Id, Is.Zero);

            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);

            bufferWriter.GetSpan(BufferSize.KB_8);
            bufferWriter.Write(bytes);

            var pos = bufferWriter.End;
            var ros = bufferWriter.AsReadOnly;

            using var ross = new ReadOnlySequenceStream(ros);

            await bufferWriter.WriteAsync(ross);

            var ros2 = bufferWriter.AsReadOnly;
            var sliced = ros2.Slice(pos);

            Assert.That(sliced.SequenceEqual(ros), Is.True);
        }
        finally
        {
            bufferWriter.Reset();
        }
    }

    [Test]
    public async Task ReturnSequenceToSharedPool_Test()
    {
        var bufferWriter = SequenceBufferWriter<byte>.Pool.Rent();

        var rentedBuffer = (IRentedBuffer<SequenceBufferWriter<byte>>)bufferWriter;
        Assert.That(rentedBuffer.BufferPool, Is.EqualTo(SequenceBufferWriter<byte>.Pool));
        Assert.That(rentedBuffer.BufferPool.Id, Is.Zero);

        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);

        bufferWriter.GetSpan(BufferSize.KB_8);
        bufferWriter.Write(bytes);

        var pos = bufferWriter.End;
        var ros = bufferWriter.AsReadOnly;

        using var ross = new ReadOnlySequenceStream(ros, ReturnSequenceToSharedPool, bufferWriter);

        await bufferWriter.WriteAsync(ross);

        var ros2 = bufferWriter.AsReadOnly;
        var sliced = ros2.Slice(pos);

        Assert.That(sliced.SequenceEqual(ros), Is.True);
    }

    private static void ReturnSequenceToSharedPool(object? arg)
    {
        var seq = (SequenceBufferWriter<byte>?)arg;
        if (seq == null) throw new ArgumentNullException(nameof(arg));

        SequenceBufferWriter<byte>.Pool.TryReturn(seq);
    }

    [Test]
    public void LeakTest()
    {
        var sequence = new SequenceBufferWriter<object>();
        var span = sequence.GetSpan(BufferSize.KB);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = new object();
        }
        sequence.Reset();
        for (int i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.Null);
        }
    }

    [Test]
    public void Test_GetSpanGetSpan()
    {
        var sequence = new SequenceBufferWriter<byte>();

        var span = sequence.GetSpan();
        var span2 = sequence.GetSpan();
        var span3 = sequence.GetSpan(span.Length + 1);
    }

    [Test]
    public void Advance_Test()
    {
        var sequence = new SequenceBufferWriter<byte>();

        Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Advance(1));

        var span = sequence.GetSpan();
        Assert.Throws<InvalidOperationException>(() => sequence.ArrayPool = null);
        sequence.Advance(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Advance(int.MaxValue));
    }

    [Test]
    public void Write_Test()
    {
        var sequence = new SequenceBufferWriter<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);

            sequence.Write(bytes);

            var ros = sequence.AsReadOnly;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.MB_2));

            var ros2 = bytes.AsMemory().ToSequence();
            Assert.That(ros.SequenceEqual(ros2), Is.True);
        }
        finally
        {
            sequence.Reset();
        }
    }

    [Test]
    public async Task WriteAsync_Test()
    {
        var sequence = new SequenceBufferWriter<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnly;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.MB_2));
        }
        finally
        {
            sequence.Reset();
        }
    }

    [Test]
    public async Task WriteAsync_OneOfEachSize_Test()
    {
        var sequence = new SequenceBufferWriter<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            sequence.NextBufferSize = BufferSize.KB_64;
            sequence.GetSpan(BufferSize.KB_128);

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnly;
            var start = sequence.End;

            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            Assert.That(ros.SequenceEqual(bytes), Is.True);
            Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.MB));

            sequence.NextBufferSize = BufferSize.KB;
            var lastBuffer = new byte[BufferSize.KB_80];
            Random.Shared.NextBytes(lastBuffer);
            sequence.Write(lastBuffer);

            ros = sequence.AsReadOnly;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length + lastBuffer.Length));
            Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.KB_32));

            var lastROS = ros.Slice(start);
            Assert.That(lastROS.Length, Is.EqualTo(lastBuffer.Length));
            Assert.That(lastROS.SequenceEqual(lastBuffer), Is.True);

            sequence.AdvanceTo(start);
            ros = sequence.AsReadOnly;
            Assert.That(ros.SequenceEqual(lastROS), Is.True);
            Assert.That(ros.Length, Is.EqualTo(lastBuffer.Length));
            Assert.That(ros.SequenceEqual(lastBuffer), Is.True);

            Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.KB_32));
        }
        finally
        {
            sequence.Reset();
        }
    }

    [Test]
    public async Task WriteAsync_TwoOfEachSize_Test()
    {
        var sequence = new SequenceBufferWriter<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            sequence.GrowthStrategy = BufferGrowthStrategy.TwoOfEachSize;

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnly;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            Assert.That(sequence.NextBufferSize, Is.EqualTo(454997));
        }
        finally
        {
            sequence.Reset();
        }
    }

    [Test]
    public async Task WriteAsync_FourOfEachSize_Test()
    {
        var sequence = new SequenceBufferWriter<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            sequence.GrowthStrategy = BufferGrowthStrategy.FourOfEachSize;

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnly;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            Assert.That(sequence.NextBufferSize, Is.EqualTo(158003));
        }
        finally
        {
            sequence.Reset();
        }
    }

    [Test]
    public async Task WriteAsync_Off_Test()
    {
        var sequence = new SequenceBufferWriter<byte>();
        try
        {
            var bytes = new byte[BufferSize.MB];
            Random.Shared.NextBytes(bytes);
            var stream = new MemoryStream(bytes);

            sequence.GrowthStrategy = BufferGrowthStrategy.Off;
            sequence.NextBufferSize = BufferSize.KB;

            await sequence.WriteAsync(stream);

            var ros = sequence.AsReadOnly;
            Assert.That(ros.Start, Is.EqualTo(sequence.Start));
            Assert.That(ros.End, Is.EqualTo(sequence.End));

            Assert.That(sequence.Length, Is.EqualTo(bytes.Length));
            Assert.That(sequence.NextBufferSize, Is.EqualTo(BufferSize.KB));
        }
        finally
        {
            sequence.Reset();
        }
    }
}