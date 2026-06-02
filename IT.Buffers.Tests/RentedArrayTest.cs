namespace IT.Buffers.Tests;

internal class RentedArrayTest
{
    [Test]
    public void Test()
    {
        var array = RentedArray<byte>.Empty;

        Assert.That(array.Equals(default), Is.False);
        Assert.That(array.Equals(RentedArray<byte>.Empty), Is.True);

        Equals(array);

        var shared = new RentedArray<byte>([], RentedArrayType.Shared);
        Equals(shared, type: RentedArrayType.Shared);

        var global = new RentedArray<byte>([], RentedArrayType.Global);
        Equals(global, type: RentedArrayType.Global);

        var external = new RentedArray<byte>([], RentedArrayType.External);
        Equals(external, type: RentedArrayType.External);

        Assert.That(array.Equals(shared), Is.False);
        Assert.That(array.Equals(global), Is.False);
        Assert.That(array.Equals(external), Is.False);

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
        Equals(rented, length: 16, count: 1, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(rented), Is.True);

        rented = BufferPool.RentArray<byte>(BufferSize.MB_32);
        Equals(rented, length: BufferSize.MB_32, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(rented), Is.True);

        rented = BufferPool.RentArray<byte>(BufferSize.GB - 1);
        Equals(rented, length: BufferSize.GB, count: BufferSize.GB - 1, type: RentedArrayType.Shared);
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

    private static void Equals(RentedArray<byte> array,
        int length = 0, int offset = 0, int count = 0,
        RentedArrayType type = RentedArrayType.None)
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