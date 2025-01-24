using IT.Buffers.Extensions;
using System.Buffers;

namespace IT.Buffers.Tests;

public class ReadOnlySequenceTest
{
    [Test]
    public void OverflowPoolTest()
    {
        var segment = new SequenceSegment<byte>();

        Assert.That(segment.IsRentedSegment, Is.False);

        Assert.That(SequenceSegment<byte>.Pool.Return(segment), Is.False);

        segment = SequenceSegment<byte>.Pool.Rent();

        Assert.That(segment.IsRentedSegment, Is.True);

        Assert.That(SequenceSegment<byte>.Pool.Return(segment), Is.True);

        Assert.That(segment.IsRentedSegment, Is.True);
    }

    [Test]
    public void Test()
    {
        for (int i = 0; i < 10; i++)
        {
            var rented = RentSequence(i);
            var buffer = rented.ToArray();
            var single = new ReadOnlySequence<byte>(buffer);

            Assert.That(rented.SequenceEqual(single));
            Assert.That(single.SequenceEqual(rented));
            Assert.That(rented.SequenceEqual(rented));

            var splitDouble = buffer.AsMemory().Split(10);
            Assert.That(rented.SequenceEqual(splitDouble));

            var splitFixed = buffer.AsMemory().Split(buffer.Length / 5, BufferGrowthPolicy.Fixed);
            Assert.That(rented.SequenceEqual(splitFixed));

            Assert.That(ArrayPoolShared.TryReturn(rented), Is.EqualTo(i));
        }
    }

    private static ReadOnlySequence<byte> RentSequence(int segments, int bufferSize = 10)
    {
        if (segments == 0) return ReadOnlySequence<byte>.Empty;

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var rented = buffer.AsMemory(0, bufferSize);
        Random.Shared.NextBytes(rented.Span);

        if (segments == 1) return new ReadOnlySequence<byte>(rented);

        var start = SequenceSegment<byte>.Pool.Rent();
        start.SetMemory(rented, isRented: true);

        var end = start;

        for (int i = 1; i < segments; i++)
        {
            var nextBufferSize = buffer.Length + bufferSize;
            buffer = ArrayPool<byte>.Shared.Rent(nextBufferSize);
            rented = buffer.AsMemory(0, nextBufferSize);
            Random.Shared.NextBytes(rented.Span);

            end = end.AppendRented(rented, isRented: true);
        }

        return new ReadOnlySequence<byte>(start, 0, end, end.Memory.Length);
    }
}