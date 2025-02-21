using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    public void TestGeneric()
    {
        Assert.That(BufferSize<byte>.Min, Is.EqualTo(BufferSize.Min));
        Assert.That(BufferSize<byte>.KB_Half, Is.EqualTo(BufferSize.KB_Half));
        Assert.That(BufferSize<byte>.KB, Is.EqualTo(BufferSize.KB));
        Assert.That(BufferSize<byte>.KB_2, Is.EqualTo(BufferSize.KB_2));
        Assert.That(BufferSize<byte>.KB_4, Is.EqualTo(BufferSize.KB_4));
        Assert.That(BufferSize<byte>.KB_8, Is.EqualTo(BufferSize.KB_8));
        Assert.That(BufferSize<byte>.KB_16, Is.EqualTo(BufferSize.KB_16));
        Assert.That(BufferSize<byte>.KB_32, Is.EqualTo(BufferSize.KB_32));
        Assert.That(BufferSize<byte>.KB_64, Is.EqualTo(BufferSize.KB_64));
        Assert.That(BufferSize<byte>.KB_80, Is.EqualTo(BufferSize.KB_80));
        Assert.That(BufferSize<byte>.KB_83, Is.EqualTo(BufferSize.KB_83));
        Assert.That(BufferSize<byte>.LOH, Is.EqualTo(BufferSize.LOH));
        Assert.That(BufferSize<byte>.KB_128, Is.EqualTo(BufferSize.KB_128));
        Assert.That(BufferSize<byte>.KB_256, Is.EqualTo(BufferSize.KB_256));
        Assert.That(BufferSize<byte>.KB_512, Is.EqualTo(BufferSize.KB_512));
        Assert.That(BufferSize<byte>.MB_Half, Is.EqualTo(BufferSize.KB_512));
        Assert.That(BufferSize<byte>.MB_Half, Is.EqualTo(BufferSize.MB_Half));
        Assert.That(BufferSize<byte>.MB, Is.EqualTo(BufferSize.MB));
        Assert.That(BufferSize<byte>.MB_2, Is.EqualTo(BufferSize.MB_2));
        Assert.That(BufferSize<byte>.MB_4, Is.EqualTo(BufferSize.MB_4));
        Assert.That(BufferSize<byte>.MB_8, Is.EqualTo(BufferSize.MB_8));
        Assert.That(BufferSize<byte>.MB_16, Is.EqualTo(BufferSize.MB_16));
        Assert.That(BufferSize<byte>.MB_32, Is.EqualTo(BufferSize.MB_32));
        Assert.That(BufferSize<byte>.MB_64, Is.EqualTo(BufferSize.MB_64));
        Assert.That(BufferSize<byte>.MB_128, Is.EqualTo(BufferSize.MB_128));
        Assert.That(BufferSize<byte>.MB_256, Is.EqualTo(BufferSize.MB_256));
        Assert.That(BufferSize<byte>.MB_512, Is.EqualTo(BufferSize.MB_512));
        Assert.That(BufferSize<byte>.GB_Half, Is.EqualTo(BufferSize.MB_512));
        Assert.That(BufferSize<byte>.GB_Half, Is.EqualTo(BufferSize.GB_Half));
        Assert.That(BufferSize<byte>.Max_Half, Is.EqualTo(BufferSize.Max_Half));
        Assert.That(BufferSize<byte>.GB, Is.EqualTo(BufferSize.GB));
        Assert.That(BufferSize<byte>.Max, Is.EqualTo(BufferSize.Max));
    }

    [Test]
    public void Log2Test()
    {
        Assert.That(BufferSize<byte>.Log2, Is.EqualTo(0));//2^0
        Assert.That(BufferSize<char>.Log2, Is.EqualTo(1));//2^1
        Assert.That(BufferSize<short>.Log2, Is.EqualTo(1));//2^1
        Assert.That(BufferSize<int>.Log2, Is.EqualTo(2));//2^2
        Assert.That(BufferSize<Guid>.Log2, Is.EqualTo(4));//2^4
    }

    [Test]
    public void SizeOfTest()
    {
        Assert.That(Unsafe.SizeOf<ArraySegment<byte>>(), Is.EqualTo(16));
        Assert.That(Unsafe.SizeOf<RentedArray<byte>>(), Is.EqualTo(16));

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

    //TODO: why not 12 bytes?
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct RentedArray<T>
    {
        private readonly T[]? _array;
        private readonly int _index;

        public RentedArray(T[]? array, int index)
        {
            _array = array;
            _index = index;
        }
    }
}