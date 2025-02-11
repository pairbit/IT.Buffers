using System.Buffers;
using System.Runtime.CompilerServices;

namespace IT.Buffers.Tests;

public class BufferSizeTest
{
    [Test]
    public void Test()
    {
        Assert.That(BufferSize<char>.KB_256, Is.EqualTo(BufferSize.KB_128));
        Assert.That(BufferSize<int>.KB_256, Is.EqualTo(BufferSize.KB_64));
        Assert.That(BufferSize<long>.KB_256, Is.EqualTo(BufferSize.KB_32));
        Assert.That(BufferSize<Guid>.KB_256, Is.EqualTo(BufferSize.KB_16));
    }

    [Test]
    public void SizeOfTest()
    {
        Assert.That(Unsafe.SizeOf<ReadOnlySequence<byte>>(), Is.EqualTo(24));
        Assert.That(Unsafe.SizeOf<SequenceSegments<byte>>(), Is.EqualTo(16));

        Assert.That(Unsafe.SizeOf<Memory<byte>>(), Is.EqualTo(16));
        Assert.That(Unsafe.SizeOf<ValueFixedMemoryBufferWriter<byte>>(), Is.EqualTo(24));

        Assert.That(Unsafe.SizeOf<byte[]>(), Is.EqualTo(8));
        Assert.That(Unsafe.SizeOf<ValueFixedArrayBufferWriter<byte>>(), Is.EqualTo(16));

#if NET9_0_OR_GREATER
        Assert.That(Unsafe.SizeOf<Span<byte>>(), Is.EqualTo(16));
        Assert.That(Unsafe.SizeOf<ValueFixedSpanBufferWriter<byte>>(), Is.EqualTo(24));
#endif
    }
}