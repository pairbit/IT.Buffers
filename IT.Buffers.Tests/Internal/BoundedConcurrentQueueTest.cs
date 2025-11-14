using IT.Buffers.Internal;

namespace IT.Buffers.Tests.Internal;

internal class BoundedConcurrentQueueTest
{
    [Test]
    public void Power2Test()
    {
        Assert.That(Power2(1), Is.EqualTo(2));
        Assert.That(Power2(2), Is.EqualTo(4));
        Assert.That(Power2(3), Is.EqualTo(8));
        Assert.That(Power2(4), Is.EqualTo(16));
        Assert.That(Power2(5), Is.EqualTo(32));
        Assert.That(Power2(20), Is.EqualTo(1024 * 1024));
        Assert.That(Power2(30), Is.EqualTo(BufferSize.GB));
    }

    [Test]
    public void EnqueueDequeueTest()
    {
        //var q = new System.Collections.Concurrent.ConcurrentQueue<byte[]>();
        var queue = new BoundedConcurrentQueue<byte[]>(1);

        Assert.That(queue.TryEnqueue(new byte[1]), Is.True);
        Assert.That(queue.TryEnqueue(new byte[2]), Is.True);
        Assert.That(queue.TryEnqueue([]), Is.False);

        Assert.That(queue.TryDequeue(out var array), Is.True);
        Assert.That(array != null && array.Length == 1, Is.True);

        Assert.That(queue.TryDequeue(out var array2), Is.True);
        Assert.That(array2 != null && array2.Length == 2, Is.True);

        Assert.That(queue.TryDequeue(out array2), Is.False);
        Assert.That(array2 == null, Is.True);
    }

    [Test]
    public void IsEmptyTest()
    {
        var queue = new BoundedConcurrentQueue<byte[]>(1);

        Assert.That(queue.IsEmpty(), Is.True);

        Assert.That(queue.TryEnqueue([]), Is.True);

        Assert.That(queue.IsEmpty(), Is.False);

        Assert.That(queue.TryDequeue(out var array), Is.True);
        Assert.That(array != null && array.Length == 0, Is.True);

        Assert.That(queue.IsEmpty(), Is.True);
    }

    [Test]
    public void FreezeTest()
    {
        var queue = new BoundedConcurrentQueue<byte[]>(1);

        Assert.That(queue.IsEmpty(), Is.True);

        Assert.That(queue.TryEnqueue(new byte[1]), Is.True);

        Assert.That(queue.IsEmpty(), Is.False);

        queue.Freeze();

        Assert.That(queue.IsEmpty(), Is.False);

        Assert.That(queue.TryEnqueue([]), Is.False);

        Assert.That(queue.TryDequeue(out var array), Is.True);
        Assert.That(array != null && array.Length == 1, Is.True);

        Assert.That(queue.IsEmpty(), Is.True);

        Assert.That(queue.TryDequeue(out array), Is.False);
        Assert.That(array == null, Is.True);
    }

    private static int Power2(int power)
    {
        var res = 2 << (power - 1);

        Assert.That(System.Numerics.BitOperations.IsPow2(res), Is.True);

        return res;
    }
}