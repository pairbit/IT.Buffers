using IT.Buffers.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IT.Buffers.Tests;

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
    public void SizeOfTest()
    {
        var type = Type.GetType("System.Collections.Concurrent.PaddedHeadAndTail");
        Assert.That(type, Is.Not.Null);

        int size = Marshal.SizeOf(type);

        Assert.That(size, Is.EqualTo(Padding.CACHE_LINE_SIZE * 3));
        Assert.That(Unsafe.SizeOf<PaddedHeadAndTail>(), Is.EqualTo(size));
    }

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
    public void FreezeTest()
    {
        var queue = new BoundedConcurrentQueue<byte[]>(1);

        Assert.That(queue.Capacity, Is.EqualTo(2));
        Assert.That(queue.IsEmpty(), Is.True);
        Assert.That(queue.GetCount(), Is.EqualTo(0));

        Assert.That(queue.TryEnqueue(new byte[1]), Is.True);
        Assert.That(queue.IsEmpty(), Is.False);
        Assert.That(queue.GetCount(), Is.EqualTo(1));

        Assert.That(queue.IsFrozen, Is.False);
        queue.Freeze();
        Assert.That(queue.IsFrozen, Is.True);

        Assert.That(queue.IsEmpty(), Is.False);
        Assert.That(queue.GetCount(), Is.EqualTo(1));

        Assert.That(queue.TryEnqueue([]), Is.False);

        Assert.That(queue.TryDequeue(out var array), Is.True);
        Assert.That(array != null && array.Length == 1, Is.True);

        Assert.That(queue.IsEmpty(), Is.True);
        Assert.That(queue.GetCount(), Is.EqualTo(0));

        Assert.That(queue.TryDequeue(out array), Is.False);
        Assert.That(array == null, Is.True);
    }

    [Test]
    public void InvalidTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedConcurrentQueue<byte[]>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedConcurrentQueue<byte[]>(0));
    }

    private static int Power2(int power)
    {
        var res = 2 << (power - 1);

        Assert.That(System.Numerics.BitOperations.IsPow2(res), Is.True);

        return res;
    }
}