namespace IT.Buffers.Tests;

internal class BufferTest
{
    [Test]
    public void Test()
    {
        var buffer = Buffer<byte>.Empty;

        Assert.That(buffer.Equals(default), Is.False);
        Assert.That(buffer.Equals(Buffer<byte>.Empty), Is.True);

        OptionsEqualTo(buffer);

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
        OptionsEqualTo(shared, arrayLength: 1, offset: 1, count: 0, type: RentedArrayType.Shared);

        global = new Buffer<byte>([1], 1, 0, RentedArrayType.Global);
        OptionsEqualTo(global, arrayLength: 1, offset: 1, count: 0, type: RentedArrayType.Global);

        external = new Buffer<byte>([1], 1, 0, RentedArrayType.External);
        OptionsEqualTo(external, arrayLength: 1, offset: 1, count: 0, type: RentedArrayType.External);
    }

    [Test]
    public void RentTest()
    {
        var buffer = BufferPool.Rent<byte>(0);
        Equals(buffer);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(1);
        OptionsEqualTo(buffer, arrayLength: 16, count: 1, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32);
        EqualTo(buffer, BufferSize.MB_32, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.GB - 1);
        OptionsEqualTo(buffer, arrayLength: BufferSize.GB, count: BufferSize.GB - 1, type: RentedArrayType.Shared);
        Assert.That(BufferPool.TryReturn(buffer), Is.True);

        buffer = BufferPool.Rent<byte>(BufferSize.GB + 1);
        EqualTo(buffer, BufferSize.GB + 1);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(0, BufferSize.MB_16);
        OptionsEqualTo(buffer);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32, BufferSize.MB_16);
        EqualTo(buffer, BufferSize.MB_32);
        Assert.That(BufferPool.TryReturn(buffer), Is.False);
    }

    private static void OptionsEqualTo(Buffer<byte> array,
        int arrayLength = 0, int offset = 0, int count = 0,
        RentedArrayType type = RentedArrayType.None)
    {
        Assert.That(array.Array != null && array.Array.Length == arrayLength, Is.True);
        Assert.That(array.Start, Is.EqualTo(offset));
        Assert.That(array.Length, Is.EqualTo(count));
        Assert.That(array.ArrayType, Is.EqualTo(type));
        Assert.That(array.IsEmpty, Is.EqualTo(count == 0));
    }

    private static void EqualTo(Buffer<byte> array, int length, int offset = 0,
        RentedArrayType type = RentedArrayType.None)
    {
        Assert.That(array.Array != null && array.Array.Length == length, Is.True);
        Assert.That(array.Start, Is.EqualTo(offset));
        Assert.That(array.Length, Is.EqualTo(length));
        Assert.That(array.ArrayType, Is.EqualTo(type));
        Assert.That(array.IsEmpty, Is.EqualTo(length == 0));
    }
}