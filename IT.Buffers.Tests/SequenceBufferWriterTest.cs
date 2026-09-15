using IT.Buffers.Extensions;
using System.Buffers;

namespace IT.Buffers.Tests;

internal class SequenceBufferWriterTest
{
    [Test]
    public async Task Pool_Test()
    {
        var bufferWriter = new SequenceBufferWriter<byte>();
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

    [Test]
    public async Task ROSS_DisposeArg_Test()
    {
        var bufferWriter = SequenceBufferWriter<byte>.Pool.Rent();

        var bytes = new byte[BufferSize.MB];
        Random.Shared.NextBytes(bytes);

        bufferWriter.GetSpan(BufferSize.KB_8);
        bufferWriter.Write(bytes);

        var pos = bufferWriter.End;
        var ros = bufferWriter.AsReadOnly;

        using var ross = new ReadOnlySequenceStream(ros, DisposeArg, bufferWriter);

        await bufferWriter.WriteAsync(ross);

        var ros2 = bufferWriter.AsReadOnly;
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

        var ros = bufferWriter.AsReadOnly;
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

        var ros = bufferWriter.AsReadOnly;
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

        var ros = bufferWriter.AsReadOnly;
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

        ros = bufferWriter.AsReadOnly;
        Assert.That(ros.Start, Is.EqualTo(bufferWriter.Start));
        Assert.That(ros.End, Is.EqualTo(bufferWriter.End));

        Assert.That(bufferWriter.Length, Is.EqualTo(bytes.Length + lastBuffer.Length));
        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(BufferSize.KB_32));

        var lastROS = ros.Slice(start);
        Assert.That(lastROS.Length, Is.EqualTo(lastBuffer.Length));
        Assert.That(lastROS.SequenceEqual(lastBuffer), Is.True);

        bufferWriter.AdvanceTo(start);
        ros = bufferWriter.AsReadOnly;
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

        var ros = bufferWriter.AsReadOnly;
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

        var ros = bufferWriter.AsReadOnly;
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

        var ros = bufferWriter.AsReadOnly;
        Assert.That(ros.Start, Is.EqualTo(bufferWriter.Start));
        Assert.That(ros.End, Is.EqualTo(bufferWriter.End));

        Assert.That(bufferWriter.Length, Is.EqualTo(bytes.Length));
        Assert.That(bufferWriter.NextBufferSize, Is.EqualTo(BufferSize.KB));
    }
}