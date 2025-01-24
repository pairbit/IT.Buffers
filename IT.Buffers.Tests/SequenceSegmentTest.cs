using System.Buffers;

namespace IT.Buffers.Tests;

public class SequenceSegmentTest
{
    [Test]
    public void Test()
    {
        for (int i = 0; i < 10; i++)
        {
            var rented = RentSequence(i);
            var returned = ArrayPoolShared.TryReturn(rented);

            Assert.That(returned, Is.EqualTo(i));
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

            end = Append(end, rented);
        }

        return new ReadOnlySequence<byte>(start, 0, end, end.Memory.Length);
    }

    private static SequenceSegment<T> Append<T>(SequenceSegment<T> segment, Memory<T> memory)
    {
        var next = SequenceSegment<T>.Pool.Rent();

        next.SetMemory(memory, true);
        next.RunningIndex = segment.RunningIndex + segment.Memory.Length;

        segment.Next = next;

        return next;
    }
}