namespace IT.Buffers.Tests;

internal class RentedBufferTest
{
    [Test]
    public void Test()
    {
        var buffer = RentedBuffer<byte>.Empty;

        Assert.That(buffer.Equals(default), Is.False);
        Assert.That(buffer.Equals(RentedBuffer<byte>.Empty), Is.True);

        Equals(buffer);

        var shared = new RentedBuffer<byte>([], RentedBufferType.Shared);
        Equals(shared, type: RentedBufferType.Shared);

        var global = new RentedBuffer<byte>([], RentedBufferType.Global);
        Equals(global, type: RentedBufferType.Global);

        var external = new RentedBuffer<byte>([], RentedBufferType.External);
        Equals(external, type: RentedBufferType.External);

        Assert.That(buffer.Equals(shared), Is.False);
        Assert.That(buffer.Equals(global), Is.False);
        Assert.That(buffer.Equals(external), Is.False);

        Assert.That(shared.Equals(global), Is.False);
        Assert.That(shared.Equals(external), Is.False);

        Assert.That(global.Equals(external), Is.False);
    }

    [Test]
    public void RentTest()
    {
        var rented = BufferPool.RentArray<byte>(0);
        Equals(rented);
        Assert.That(BufferPool.TryReturn(rented), Is.False);

        rented = BufferPool.RentArray<byte>(1);
        Equals(rented, length: 16, count: 1, type: RentedBufferType.Shared);
        Assert.That(BufferPool.TryReturn(rented), Is.True);

        rented = BufferPool.RentArray<byte>(BufferSize.MB_32);
        Equals(rented, length: BufferSize.MB_32, type: RentedBufferType.Shared);
        Assert.That(BufferPool.TryReturn(rented), Is.True);

        rented = BufferPool.RentArray<byte>(BufferSize.GB - 1);
        Equals(rented, length: BufferSize.GB, count: BufferSize.GB - 1, type: RentedBufferType.Shared);
        Assert.That(BufferPool.TryReturn(rented), Is.True);

        rented = BufferPool.RentArray<byte>(BufferSize.GB + 1);
        Equals(rented, length: BufferSize.GB + 1);
        Assert.That(BufferPool.TryReturn(rented), Is.False);

        rented = BufferPool.RentArray<byte>(0, BufferSize.MB_16);
        Equals(rented);
        Assert.That(BufferPool.TryReturn(rented), Is.False);

        rented = BufferPool.RentArray<byte>(BufferSize.MB_32, BufferSize.MB_16);
        Equals(rented, length: BufferSize.MB_32);
        Assert.That(BufferPool.TryReturn(rented), Is.False);
    }

    private static void Equals(RentedBuffer<byte> array,
        int length = 0, int offset = 0, int count = 0,
        RentedBufferType type = RentedBufferType.None)
    {
        Assert.That(array.Array != null && array.Array.Length == length, Is.True);

        if (count == 0)
        {
            count = length;
        }
        Assert.That(array.Offset, Is.EqualTo(offset));
        Assert.That(array.Count, Is.EqualTo(count));
        Assert.That(array.Type, Is.EqualTo(type));
    }
}