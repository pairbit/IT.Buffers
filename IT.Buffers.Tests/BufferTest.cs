namespace IT.Buffers.Tests;

internal class BufferTest
{
    [Test]
    public void Test()
    {
        var buffer = Buffer<byte>.Empty;

        Assert.That(buffer.Equals(default), Is.False);
        Assert.That(buffer.Equals(Buffer<byte>.Empty), Is.True);

        EqualTo(buffer);

        var none = new Buffer<byte>([1]);
        EqualTo(none, 1);

        var shared = new Buffer<byte>([1], RentedArrayType.Shared);
        EqualTo(shared, 1, type: RentedArrayType.Shared);

        var global = new Buffer<byte>([1], RentedArrayType.Global);
        EqualTo(global, 1, type: RentedArrayType.Global);

        var external = new Buffer<byte>([1], RentedArrayType.External);
        EqualTo(external, 1, type: RentedArrayType.External);

        Assert.That(buffer.Equals(shared), Is.False);
        Assert.That(buffer.Equals(global), Is.False);
        Assert.That(buffer.Equals(external), Is.False);

        Assert.That(shared.Equals(global), Is.False);
        Assert.That(shared.Equals(external), Is.False);

        Assert.That(global.Equals(external), Is.False);

        shared = new Buffer<byte>([1], 1, 0, RentedArrayType.Shared);
        EqualTo(shared, arrayLength: 1, offset: 1, count: 0, type: RentedArrayType.Shared);

        global = new Buffer<byte>([1], 1, 0, RentedArrayType.Global);
        EqualTo(global, arrayLength: 1, offset: 1, count: 0, type: RentedArrayType.Global);

        external = new Buffer<byte>([1], 1, 0, RentedArrayType.External);
        EqualTo(external, arrayLength: 1, offset: 1, count: 0, type: RentedArrayType.External);
    }

    [Test]
    public void RentTest()
    {
        var buffer = BufferPool.Rent<byte>(0);
        Equals(buffer);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(1);
        EqualTo(buffer, arrayLength: 16, count: 1, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32);
        EqualTo(buffer, BufferSize.MB_32, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.GB - 1);
        EqualTo(buffer, arrayLength: BufferSize.GB, count: BufferSize.GB - 1, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.GB + 1);
        EqualTo(buffer, BufferSize.GB + 1);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(0, BufferSize.MB_16);
        EqualTo(buffer);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32, BufferSize.MB_16);
        EqualTo(buffer, BufferSize.MB_32);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);
    }

    private static void EqualTo(Buffer<byte> array,
        int arrayLength = 0, int offset = 0, int count = -1,
        RentedArrayType type = RentedArrayType.None)
    {
        if (count < 0)
        {
            count = arrayLength;
        }

        Assert.That(array.Array != null && array.Array.Length == arrayLength, Is.True);
        Assert.That(array.Start, Is.EqualTo(offset));
        Assert.That(array.Length, Is.EqualTo(count));
        Assert.That(array.ArrayType, Is.EqualTo(type));
        Assert.That(array.IsEmpty, Is.EqualTo(count == 0));
    }
}