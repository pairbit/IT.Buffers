namespace IT.Buffers.Tests;

internal class BoundedConcurrentQueueTest
{
    [Test]
    public void EnqueueDequeueTest()
    {
        var queue = new BoundedConcurrentQueue<byte[]>(1);
        
        Assert.That(queue.Capacity, Is.EqualTo(2));
        Assert.That(queue.TryEnqueue(new byte[1]), Is.True);
        Assert.That(queue.TryEnqueue(new byte[2]), Is.True);
        Assert.That(queue.TryEnqueue([]), Is.False);

        Assert.That(queue.TryDequeue(out var array), Is.True);
        Assert.That(array != null && array.Length == 1, Is.True);

        Assert.That(queue.TryDequeue(out array), Is.True);
        Assert.That(array != null && array.Length == 2, Is.True);

        Assert.That(queue.TryDequeue(out array), Is.False);
        Assert.That(array == null, Is.True);
    }

    [Test]
    public void IsEmptyTest()
    {
        var queue = new BoundedConcurrentQueue<byte[]>(1);
        
        Assert.That(queue.Capacity, Is.EqualTo(2));
        Assert.That(queue.IsEmpty(), Is.True);
        Assert.That(queue.GetCount(), Is.EqualTo(0));

        Assert.That(queue.TryEnqueue([]), Is.True);
        
        Assert.That(queue.IsEmpty(), Is.False);
        Assert.That(queue.GetCount(), Is.EqualTo(1));

        Assert.That(queue.TryDequeue(out var array), Is.True);
        Assert.That(array != null && array.Length == 0, Is.True);

        Assert.That(queue.IsEmpty(), Is.True);
        Assert.That(queue.GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void InvalidTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedConcurrentQueue<byte[]>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedConcurrentQueue<byte[]>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedConcurrentQueue<byte[]>(31));
    }
}