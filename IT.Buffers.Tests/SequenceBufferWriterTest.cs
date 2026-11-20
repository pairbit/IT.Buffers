using IT.Buffers.Extensions;
using System.Buffers;

namespace IT.Buffers.Tests;

internal class SequenceBufferWriterTest
{
    [Test]
    public async Task AppendMemoryOwner_Test()
    {
        var bytes = new byte[32];
        Random.Shared.NextBytes(bytes);
        var stream = new MemoryStream(bytes);

        var memoryOwner = MemoryPool<byte>.Shared.Rent(16);

        var read = await stream.ReadAtLeastAsync(memoryOwner.Memory, memoryOwner.Memory.Length, throwOnEndOfStream: false);
        Assert.That(read, Is.EqualTo(memoryOwner.Memory.Length));

        var bufferWriter = new SequenceBufferWriter<byte>();
        Assert.That(bufferWriter.Sequence.IsEmpty, Is.True);

        bufferWriter.AppendMemoryOwner(memoryOwner);

        Assert.That(bufferWriter.Sequence.SequenceEqual(memoryOwner.Memory.Span), Is.True);

        var memory = bufferWriter.GetMemory();
        read = await stream.ReadAtLeastAsync(memory, memory.Length, throwOnEndOfStream: false);
        Assert.That(read, Is.EqualTo(16));
        bufferWriter.Advance(read);

        Assert.That(bufferWriter.Sequence.SequenceEqual(bytes), Is.True);
        bufferWriter.Reset();
    }

    [Test]
    public async Task AppendArray_Test()
    {
        var bytes = new byte[32];
        Random.Shared.NextBytes(bytes);
        var stream = new MemoryStream(bytes);

        var array = ArrayPool<byte>.Shared.Rent(16);

        var read = await stream.ReadAtLeastAsync(array, array.Length, throwOnEndOfStream: false);
        Assert.That(read, Is.EqualTo(array.Length));

        var bufferWriter = new SequenceBufferWriter<byte>();
        Assert.That(bufferWriter.Sequence.IsEmpty, Is.True);

        bufferWriter.AppendArray(array);

        Assert.That(bufferWriter.Sequence.SequenceEqual(array), Is.True);

        var memory = bufferWriter.GetMemory();
        read = await stream.ReadAtLeastAsync(memory, memory.Length, throwOnEndOfStream: false);
        Assert.That(read, Is.EqualTo(16));
        bufferWriter.Advance(read);

        Assert.That(bufferWriter.Sequence.SequenceEqual(bytes), Is.True);
        bufferWriter.Reset();
    }

    [Test]
    public async Task Write_LengthStream_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();

        var memory = bufferWriter.GetMemory(11).Slice(0, 11);//max length int
        memory.Span.Clear();
        memory.Span[0] = (byte)'$';
        bufferWriter.Advance(11);

        var stream = new MemoryStream("MyValue"u8.ToArray());

        var length = bufferWriter.Length;
        await bufferWriter.WriteAsync(stream);
        var streamLength = bufferWriter.Length - length;

        Assert.That(streamLength.TryFormat(memory.Span.Slice(1), out var written), Is.True);
        Assert.That(written, Is.EqualTo(1));

        var seq = bufferWriter.Sequence;
        Assert.That(seq.SequenceEqual("$7\0\0\0\0\0\0\0\0\0MyValue"u8), Is.True);
    }

    [Test]
    public void AdvanceTo_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();
        var span = bufferWriter.GetSpan();

        var bytes = new byte[span.Length];
        Random.Shared.NextBytes(bytes);
        bytes.CopyTo(span);
        bufferWriter.Advance(span.Length);

        var seq = bufferWriter.Sequence;
        Assert.That(seq.SequenceEqual(bytes), Is.True);

        var offset = 1;
        bufferWriter.AdvanceTo(seq.GetPosition(offset));
        Assert.That(bufferWriter.Sequence.SequenceEqual(bytes.AsSpan(offset)), Is.True);
    }

    [Test]
    public async Task Pool_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();
        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);

        bufferWriter.GetSpan(BufferSize.KB_8);
        bufferWriter.Write(bytes);

        var pos = bufferWriter.End;
        var ros = bufferWriter.Sequence;

        using var ross = new ReadOnlySequenceStream(ros);

        await bufferWriter.WriteAsync(ross);

        var ros2 = bufferWriter.Sequence;
        var sliced = ros2.Slice(pos);

        Assert.That(sliced.SequenceEqual(ros), Is.True);
    }

    [Test]
    public async Task ROSS_DisposeArg_Test()
    {
        var bufferWriter = SequenceBufferWriter<byte>.Pool.Rent();

        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);

        bufferWriter.GetSpan(BufferSize.KB_8);
        bufferWriter.Write(bytes);

        var pos = bufferWriter.End;
        var ros = bufferWriter.Sequence;

        using var ross = new ReadOnlySequenceStream(ros, DisposeArg, bufferWriter);

        await bufferWriter.WriteAsync(ross);

        var ros2 = bufferWriter.Sequence;
        var sliced = ros2.Slice(pos);

        Assert.That(sliced.SequenceEqual(ros), Is.True);
    }

    private static void DisposeArg(object? arg)
    {
        var seq = (SequenceBufferWriter<byte>?)arg;
        if (seq == null) throw new ArgumentNullException(nameof(arg));

        SequenceBufferWriter<byte>.Pool.Return(seq);
    }

    [Test]
    public void LeakTest()
    {
        var bufferWriter = new SequenceBufferWriter<object>();
        var span = bufferWriter.GetSpan(BufferSize.KB);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = new object();
        }
        bufferWriter.Reset();
        for (int i = 0; i < span.Length; i++)
        {
            Assert.That(span[i], Is.Null);
        }
    }

    [Test]
    public void Test_GetSpanGetSpan()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();

        var span = bufferWriter.GetSpan();
        var span2 = bufferWriter.GetSpan();
        var span3 = bufferWriter.GetSpan(span.Length + 1);
    }

    [Test]
    public void Advance_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();

        Assert.Throws<ArgumentOutOfRangeException>(() => bufferWriter.Advance(1));

        var span = bufferWriter.GetSpan();
        Assert.Throws<InvalidOperationException>(() => bufferWriter.ArrayPool = null);
        bufferWriter.Advance(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => bufferWriter.Advance(int.MaxValue));
    }

    [Test]
    public void Write_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();

        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);

        bufferWriter.Write(bytes);

        var ros = bufferWriter.Sequence;
        Assert.That(ros.Start, Is.EqualTo(bufferWriter.Start));
        Assert.That(ros.End, Is.EqualTo(bufferWriter.End));

        Assert.That(bufferWriter.Length, Is.EqualTo(bytes.Length));
        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(BufferSize.MB_2));

        var ros2 = bytes.AsMemory().ToSequence();
        Assert.That(ros.SequenceEqual(ros2), Is.True);
    }

    [Test]
    public async Task WriteAsync_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();
        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);
        var stream = new MemoryStream(bytes);

        await bufferWriter.WriteAsync(stream);

        var ros = bufferWriter.Sequence;
        Assert.That(ros.Start, Is.EqualTo(bufferWriter.Start));
        Assert.That(ros.End, Is.EqualTo(bufferWriter.End));

        Assert.That(bufferWriter.Length, Is.EqualTo(bytes.Length));
        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(BufferSize.MB_2));
    }

    [Test]
    public async Task WriteAsync_OneOfEachSize_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();

        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);
        var stream = new MemoryStream(bytes);

        bufferWriter.NextBufferSize = BufferSize.KB_64;
        bufferWriter.GetSpan(BufferSize.KB_128);

        await bufferWriter.WriteAsync(stream);

        var ros = bufferWriter.Sequence;
        var start = bufferWriter.End;

        Assert.That(ros.Start, Is.EqualTo(bufferWriter.Start));
        Assert.That(ros.End, Is.EqualTo(bufferWriter.End));

        Assert.That(bufferWriter.Length, Is.EqualTo(bytes.Length));
        Assert.That(ros.SequenceEqual(bytes), Is.True);
        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(BufferSize.MB));

        bufferWriter.NextBufferSize = BufferSize.KB;
        var lastBuffer = new byte[BufferSize.KB_80];
        Random.Shared.NextBytes(lastBuffer);
        bufferWriter.Write(lastBuffer);

        ros = bufferWriter.Sequence;
        Assert.That(ros.Start, Is.EqualTo(bufferWriter.Start));
        Assert.That(ros.End, Is.EqualTo(bufferWriter.End));

        Assert.That(bufferWriter.Length, Is.EqualTo(bytes.Length + lastBuffer.Length));
        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(BufferSize.KB_32));

        var lastROS = ros.Slice(start);
        Assert.That(lastROS.Length, Is.EqualTo(lastBuffer.Length));
        Assert.That(lastROS.SequenceEqual(lastBuffer), Is.True);

        bufferWriter.AdvanceTo(start);
        ros = bufferWriter.Sequence;
        Assert.That(ros.SequenceEqual(lastROS), Is.True);
        Assert.That(ros.Length, Is.EqualTo(lastBuffer.Length));
        Assert.That(ros.SequenceEqual(lastBuffer), Is.True);

        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(BufferSize.KB_32));
    }

    [Test]
    public async Task WriteAsync_TwoOfEachSize_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();

        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);
        var stream = new MemoryStream(bytes);

        bufferWriter.GrowthStrategy = BufferGrowthStrategy.TwoOfEachSize;

        await bufferWriter.WriteAsync(stream);

        var ros = bufferWriter.Sequence;
        Assert.That(ros.Start, Is.EqualTo(bufferWriter.Start));
        Assert.That(ros.End, Is.EqualTo(bufferWriter.End));

        Assert.That(bufferWriter.Length, Is.EqualTo(bytes.Length));
        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(454997));
    }

    [Test]
    public async Task WriteAsync_FourOfEachSize_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();

        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);
        var stream = new MemoryStream(bytes);

        bufferWriter.GrowthStrategy = BufferGrowthStrategy.FourOfEachSize;

        await bufferWriter.WriteAsync(stream);

        var ros = bufferWriter.Sequence;
        Assert.That(ros.Start, Is.EqualTo(bufferWriter.Start));
        Assert.That(ros.End, Is.EqualTo(bufferWriter.End));

        Assert.That(bufferWriter.Length, Is.EqualTo(bytes.Length));
        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(158003));
    }

    [Test]
    public async Task WriteAsync_Off_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();

        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);
        var stream = new MemoryStream(bytes);

        bufferWriter.GrowthStrategy = BufferGrowthStrategy.Off;
        bufferWriter.NextBufferSize = BufferSize.KB;

        await bufferWriter.WriteAsync(stream);

        var ros = bufferWriter.Sequence;
        Assert.That(ros.Start, Is.EqualTo(bufferWriter.Start));
        Assert.That(ros.End, Is.EqualTo(bufferWriter.End));

        Assert.That(bufferWriter.Length, Is.EqualTo(bytes.Length));
        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(BufferSize.KB));
    }
}