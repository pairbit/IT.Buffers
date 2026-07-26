namespace IT.Buffers.Tests;

internal class BufferTest
{
    [Test]
    public void Test()
    {
        var buffer = Buffer<byte>.Empty;

        Assert.That(buffer.Equals(default), Is.False);
        Assert.That(buffer.Equals(Buffer<byte>.Empty), Is.True);

        Equals(buffer);

        var shared = new Buffer<byte>([], RentedArrayType.Shared);
        Equals(shared, type: RentedArrayType.Shared);

        var global = new Buffer<byte>([], RentedArrayType.Global);
        Equals(global, type: RentedArrayType.Global);

        var external = new Buffer<byte>([], RentedArrayType.External);
        Equals(external, type: RentedArrayType.External);

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
        var buffer = BufferPool.Rent<byte>(0);
        Equals(buffer);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(1);
        Equals(buffer, length: 16, count: 1, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32);
        Equals(buffer, length: BufferSize.MB_32, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.GB - 1);
        Equals(buffer, length: BufferSize.GB, count: BufferSize.GB - 1, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.GB + 1);
        Equals(buffer, length: BufferSize.GB + 1);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(0, BufferSize.MB_16);
        Equals(buffer);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32, BufferSize.MB_16);
        Equals(buffer, length: BufferSize.MB_32);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);
    }

    private static void Equals(Buffer<byte> array,
        int length = 0, int offset = 0, int count = 0,
        RentedArrayType type = RentedArrayType.None)
    {
        Assert.That(array.Array != null && array.Array.Length == length, Is.True);

        if (count == 0)
        {
            count = length;
        }
        Assert.That(array.Start, Is.EqualTo(offset));
        Assert.That(array.Length, Is.EqualTo(count));
        Assert.That(array.RentedArrayType, Is.EqualTo(type));
    }
}