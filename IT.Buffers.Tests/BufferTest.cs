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

        var none = new Buffer<byte>([1]);
        Equals(none, arrayLength: 1);

        var shared = new Buffer<byte>([1], RentedArrayType.Shared);
        Equals(shared, arrayLength: 1, type: RentedArrayType.Shared);

        var global = new Buffer<byte>([1], RentedArrayType.Global);
        Equals(global, arrayLength: 1, type: RentedArrayType.Global);

        var external = new Buffer<byte>([1], RentedArrayType.External);
        Equals(external, arrayLength: 1, type: RentedArrayType.External);

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
        Equals(buffer, arrayLength: 16, count: 1, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32);
        Equals(buffer, arrayLength: BufferSize.MB_32, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.GB - 1);
        Equals(buffer, arrayLength: BufferSize.GB, count: BufferSize.GB - 1, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.GB + 1);
        Equals(buffer, arrayLength: BufferSize.GB + 1);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(0, BufferSize.MB_16);
        Equals(buffer);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32, BufferSize.MB_16);
        Equals(buffer, arrayLength: BufferSize.MB_32);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);
    }

    private static void Equals(Buffer<byte> array,
        int arrayLength = 0, int offset = 0, int count = 0,
        RentedArrayType type = RentedArrayType.None)
    {
        Assert.That(array.Array != null && array.Array.Length == arrayLength, Is.True);

        if (count == 0)
        {
            count = arrayLength;
        }
        Assert.That(array.Start, Is.EqualTo(offset));
        Assert.That(array.Length, Is.EqualTo(count));
        Assert.That(array.ArrayType, Is.EqualTo(type));
        Assert.That(array.IsEmpty, Is.EqualTo(count == 0));
    }
}