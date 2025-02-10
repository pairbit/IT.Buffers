using IT.Buffers.Extensions;
using System.Buffers;

namespace IT.Buffers.Tests;

public class RentedBuffersTest
{
    public void Test()
    {
        var buffers = new RentedBuffers();

        Assert.That(buffers.Track(), Is.EqualTo(1));

        int added;
        try
        {
            added = AddBuffers(buffers);
        }
        catch
        {
            var returned = buffers.ReturnAndClear();

            Assert.That(returned, Is.EqualTo(3));

            throw;
        }

        var clear = buffers.Clear();

        Assert.That(clear, Is.EqualTo(added));
    }

    private static int AddBuffers(RentedBuffers buffers)
    {
        var count = 0;

        if (buffers.AddArray(ArrayPool<int>.Shared.Rent(32))) count++;

        if (buffers.AddArray(ArrayPool<byte>.Shared.Rent(64))) count++;

        var array = ArrayPool<byte>.Shared.Rent(128);
        var seq = array.AsMemory().SplitAndRent(32, isRented: true);

        if (buffers.AddSequence(seq)) count++;

        return count;
    }
}