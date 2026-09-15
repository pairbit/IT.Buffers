using System.Buffers;
using System.Runtime.InteropServices;

namespace IT.Buffers.Tests;

internal class BufferTest
{
    [Test]
    public void InvalidTest()
    {
        ArgumentException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Buffer<byte>([], (RentedArrayType)12));

        Assert.That(ex.ParamName, Is.EqualTo("arrayType"));
        Assert.That(ex.Message, Is.EqualTo("Specified argument was out of the range of valid values. (Parameter 'arrayType')"));

        ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Buffer<byte>(default(ArraySegment<byte>), (RentedArrayType)12));

        Assert.That(ex.ParamName, Is.EqualTo("arrayType"));
        Assert.That(ex.Message, Is.EqualTo("Specified argument was out of the range of valid values. (Parameter 'arrayType')"));

        ex = Assert.Throws<ArgumentException>(() =>
            new Buffer<byte>([], RentedArrayType.Shared));

        Assert.That(ex.ParamName, Is.EqualTo("arrayType"));
        Assert.That(ex.Message, Is.EqualTo("Empty array cannot be rented. (Parameter 'arrayType')"));

        ex = Assert.Throws<ArgumentException>(() =>
            new Buffer<byte>(default(ArraySegment<byte>), RentedArrayType.Shared));

        Assert.That(ex.ParamName, Is.EqualTo("arrayType"));
        Assert.That(ex.Message, Is.EqualTo("Empty array cannot be rented. (Parameter 'arrayType')"));

        var str = "str";
        var strMemory = MemoryMarshal.AsMemory(str.AsMemory());

        ex = Assert.Throws<ArgumentException>(() => new Buffer<char>(strMemory));

        Assert.That(ex.ParamName, Is.EqualTo("memory"));
        Assert.That(ex.Message, Is.EqualTo("Unrecognized memory type. (Parameter 'memory')"));

        var buffer = new Buffer<byte>(new byte[1], RentedArrayType.Global);
        Assert.Throws<NotImplementedException>(() => buffer.TryReturn(out _));
        Assert.Throws<NotImplementedException>(buffer.Return);

        buffer = new Buffer<byte>(new byte[1], RentedArrayType.External);
        Assert.That(Assert.Throws<InvalidOperationException>(buffer.Return).Message,
            Is.EqualTo("It is impossible to return an external array."));
    }

    [Test]
    public void EmptyTest()
    {
        Memory<byte> memory = default;
        Assert.That(memory.Slice(0).IsEmpty, Is.True);
        Assert.That(memory.Slice(0, 0).IsEmpty, Is.True);

        Assert.That(ReferenceEquals(Array.Empty<byte>(), ArraySegment<byte>.Empty.Array), Is.False);
        Assert.That(ReferenceEquals(Array.Empty<byte>(), Buffer<byte>.Empty.Array), Is.False);
        Assert.That(ReferenceEquals(ArraySegment<byte>.Empty.Array, Buffer<byte>.Empty.Array), Is.False);

        byte[] empty1 = [];
        byte[] empty2 = [];
        Assert.That(ReferenceEquals(empty1, empty2), Is.True);
        Assert.That(ReferenceEquals(empty1, Array.Empty<byte>()), Is.True);

#pragma warning disable IDE0300 // Simplify collection initialization
#pragma warning disable CA1825 // Avoid zero-length array allocations
        empty1 = new byte[0];
        empty2 = new byte[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations
#pragma warning restore IDE0300 // Simplify collection initialization

        Assert.That(ReferenceEquals(empty1, empty2), Is.False);
        Assert.That(ReferenceEquals(empty1, Buffer<byte>.Empty.Array), Is.False);
        Assert.That(ReferenceEquals(empty1, Array.Empty<byte>()), Is.False);
        Assert.That(ReferenceEquals(empty1, (byte[])[]), Is.False);

        var buffer1 = new Buffer<byte>(empty1);
        var buffer2 = new Buffer<byte>(empty2);
        Assert.That(buffer1.Equals(buffer2), Is.False);

        var memory1 = new Memory<byte>(empty1);
        var memory2 = new Memory<byte>(empty2);

        buffer1 = new Buffer<byte>(memory1);
        buffer2 = new Buffer<byte>(memory2);

        Assert.That(buffer1.Equals(buffer2), Is.False);

        Assert.That(MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory1, out var segment1), Is.True);
        Assert.That(MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory2, out var segment2), Is.True);

        Assert.That(segment1.Equals(segment2), Is.False);

        buffer1 = new Buffer<byte>(segment1);
        buffer2 = new Buffer<byte>(segment2);

        Assert.That(buffer1.Equals(buffer2), Is.False);
    }

    [Test]
    public void Test()
    {
        Assert.That(default(BufferType), Is.EqualTo(BufferType.Null));

        var buffer = Buffer<byte>.Empty;

        Assert.That(buffer.Equals(default), Is.False);
        Assert.That(buffer.Equals(Buffer<byte>.Empty), Is.True);

        EqualTo(buffer, BufferType.Array);

        var none = new Buffer<byte>([1]);
        EqualTo(none, BufferType.Array, 1);

        var shared = new Buffer<byte>([1], RentedArrayType.Shared);
        EqualTo(shared, BufferType.Array, 1, arrayType: RentedArrayType.Shared);

        var global = new Buffer<byte>([1], RentedArrayType.Global);
        EqualTo(global, BufferType.Array, 1, arrayType: RentedArrayType.Global);

        var external = new Buffer<byte>([1], RentedArrayType.External);
        EqualTo(external, BufferType.Array, 1, arrayType: RentedArrayType.External);

        Assert.That(buffer.Equals(shared), Is.False);
        Assert.That(buffer.Equals(global), Is.False);
        Assert.That(buffer.Equals(external), Is.False);

        Assert.That(shared.Equals(global), Is.False);
        Assert.That(shared.Equals(external), Is.False);

        Assert.That(global.Equals(external), Is.False);

        shared = new Buffer<byte>([1], 1, 0, RentedArrayType.Shared);
        EqualTo(shared, BufferType.Array, objLength: 1, start: 1, length: 0, arrayType: RentedArrayType.Shared);

        global = new Buffer<byte>([1], 1, 0, RentedArrayType.Global);
        EqualTo(global, BufferType.Array, objLength: 1, start: 1, length: 0, arrayType: RentedArrayType.Global);

        external = new Buffer<byte>([1], 1, 0, RentedArrayType.External);
        EqualTo(external, BufferType.Array, objLength: 1, start: 1, length: 0, arrayType: RentedArrayType.External);

        buffer = new Buffer<byte>(new byte[10], 5, 4);
        buffer[0] = 1;
        Assert.That(buffer[0], Is.EqualTo(1));
        buffer[1] = 2;
        Assert.That(buffer[1], Is.EqualTo(2));
        buffer[2] = 3;
        Assert.That(buffer[2], Is.EqualTo(3));
        buffer[3] = 4;
        Assert.That(buffer[3], Is.EqualTo(4));

        Assert.That(buffer.Span.SequenceEqual([(byte)1, (byte)2, (byte)3, (byte)4]), Is.True);

        Assert.That(buffer.AsMemory(2).Span.SequenceEqual(buffer.AsSpan(2)), Is.True);
        Assert.That(buffer.AsMemory(1, 2).Span.SequenceEqual(buffer.AsSpan(1, 2)), Is.True);

        Assert.That(buffer.Slice(2).Span.SequenceEqual(buffer.Span.Slice(2)), Is.True);
        Assert.That(buffer.Slice(1, 3).Span.SequenceEqual(buffer.Span.Slice(1, 3)), Is.True);

        Assert.That(buffer.AsMemory(2).Span.SequenceEqual(buffer.Memory.Span.Slice(2)), Is.True);
        Assert.That(buffer.AsMemory(1, 3).Span.SequenceEqual(buffer.Memory.Span.Slice(1, 3)), Is.True);

        Assert.That(buffer.AsUnrented(2), Is.EqualTo(buffer.Slice(2)));
        Assert.That(buffer.AsUnrented(1, 3), Is.EqualTo(buffer.Slice(1, 3)));

        Assert.That(buffer.AsEmpty(), Is.EqualTo(buffer.Slice(0, 0)));
    }

    [Test]
    public void TryReturnTest()
    {
        Buffer<byte> buffer = default;
        EqualTo(buffer, BufferType.Null);
        Assert.That(buffer.TryReturn(out var externalArray), Is.False);
        Assert.That(externalArray, Is.Null);

        buffer = BufferPool.Rent<byte>(0);
        EqualTo(buffer, BufferType.Array);
        Assert.That(buffer.TryReturn(out externalArray), Is.False);
        Assert.That(externalArray, Is.Null);

        buffer = BufferPool.Rent<byte>(1);
        EqualTo(buffer, BufferType.Array, objLength: 16, length: 1, arrayType: RentedArrayType.Shared);
        Assert.That(buffer.TryReturn(out externalArray), Is.True);
        Assert.That(externalArray, Is.Null);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32);
        EqualTo(buffer, BufferType.Array, BufferSize.MB_32, arrayType: RentedArrayType.Shared);
        Assert.That(buffer.TryReturn(out externalArray), Is.True);
        Assert.That(externalArray, Is.Null);

        buffer = BufferPool.Rent<byte>(BufferSize.GB - 1);
        EqualTo(buffer, BufferType.Array, objLength: BufferSize.GB, length: BufferSize.GB - 1, arrayType: RentedArrayType.Shared);
        Assert.That(buffer.TryReturn(out externalArray), Is.True);
        Assert.That(externalArray, Is.Null);

        buffer = BufferPool.Rent<byte>(BufferSize.GB + 1);
        EqualTo(buffer, BufferType.Array, BufferSize.GB + 1);
        Assert.That(buffer.TryReturn(out externalArray), Is.False);
        Assert.That(externalArray, Is.Null);

        buffer = BufferPool.Rent<byte>(0, BufferSize.MB_16);
        EqualTo(buffer, BufferType.Array);
        Assert.That(buffer.TryReturn(out externalArray), Is.False);
        Assert.That(externalArray, Is.Null);

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32, BufferSize.MB_16);
        EqualTo(buffer, BufferType.Array, BufferSize.MB_32);
        Assert.That(buffer.TryReturn(out externalArray), Is.False);
        Assert.That(externalArray, Is.Null);

        buffer = new Buffer<byte>(MemoryPool<byte>.Shared.Rent(1));
        EqualTo(buffer, BufferType.MemoryOwner, 16, start: 0, length: 16, isRented: true);
        Assert.That(buffer.TryReturn(out externalArray), Is.True);
        Assert.That(externalArray, Is.Null);

        buffer = new Buffer<byte>(MemoryPool<byte>.Shared.Rent(16), 10, 4);
        EqualTo(buffer, BufferType.MemoryOwner, 16, start: 10, length: 4, isRented: true);
        Assert.That(buffer.TryReturn(out externalArray), Is.True);
        Assert.That(externalArray, Is.Null);

        var mm = new UnmanagedMemoryManager<byte>(18);
        buffer = new Buffer<byte>(mm);
        EqualTo(buffer, BufferType.MemoryManager, 18, isRented: true);
        Assert.That(buffer.TryReturn(out externalArray), Is.True);
        Assert.That(externalArray, Is.Null);

        mm = new UnmanagedMemoryManager<byte>(22);
        buffer = new Buffer<byte>(mm, 8, 5);
        EqualTo(buffer, BufferType.MemoryManager, 22, start: 8, length: 5, isRented: true);
        Assert.That(buffer.TryReturn(out externalArray), Is.True);
        Assert.That(externalArray, Is.Null);

        var array = new byte[1];
        buffer = new Buffer<byte>(array, RentedArrayType.External);
        EqualTo(buffer, BufferType.Array, objLength: 1, arrayType: RentedArrayType.External);
        Assert.That(buffer.TryReturn(out externalArray), Is.False);
        Assert.That(externalArray, Is.EqualTo(array));
    }

    [Test]
    public void ReturnTest()
    {
        Buffer<byte> buffer = default;
        Assert.That(buffer.Type, Is.EqualTo(BufferType.Null));
        EqualTo(buffer, BufferType.Null);
        buffer.Return();

        buffer = BufferPool.Rent<byte>(0);
        EqualTo(buffer, BufferType.Array);
        buffer.Return();

        buffer = BufferPool.Rent<byte>(1);
        EqualTo(buffer, BufferType.Array, objLength: 16, length: 1, arrayType: RentedArrayType.Shared);
        buffer.Return();

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32);
        EqualTo(buffer, BufferType.Array, BufferSize.MB_32, arrayType: RentedArrayType.Shared);
        buffer.Return();

        buffer = BufferPool.Rent<byte>(BufferSize.GB - 1);
        EqualTo(buffer, BufferType.Array, objLength: BufferSize.GB, length: BufferSize.GB - 1, arrayType: RentedArrayType.Shared);
        buffer.Return();

        buffer = BufferPool.Rent<byte>(BufferSize.GB + 1);
        EqualTo(buffer, BufferType.Array, BufferSize.GB + 1);
        buffer.Return();

        buffer = BufferPool.Rent<byte>(0, BufferSize.MB_16);
        EqualTo(buffer, BufferType.Array);
        buffer.Return();

        buffer = BufferPool.Rent<byte>(BufferSize.MB_32, BufferSize.MB_16);
        EqualTo(buffer, BufferType.Array, BufferSize.MB_32);
        buffer.Return();

        buffer = new Buffer<byte>(MemoryPool<byte>.Shared.Rent(1));
        EqualTo(buffer, BufferType.MemoryOwner, 16, start: 0, length: 16, isRented: true);
        buffer.Return();

        buffer = new Buffer<byte>(MemoryPool<byte>.Shared.Rent(16), 10, 4);
        EqualTo(buffer, BufferType.MemoryOwner, 16, start: 10, length: 4, isRented: true);
        buffer.Return();

        var mm = new UnmanagedMemoryManager<byte>(18);
        buffer = new Buffer<byte>(mm);
        EqualTo(buffer, BufferType.MemoryManager, 18, isRented: true);
        buffer.Return();

        mm = new UnmanagedMemoryManager<byte>(22);
        buffer = new Buffer<byte>(mm, 8, 5);
        EqualTo(buffer, BufferType.MemoryManager, 22, start: 8, length: 5, isRented: true);
        buffer.Return();
    }

    private static void EqualTo(Buffer<byte> buffer, BufferType type,
        int objLength = 0, int start = 0, int length = -1,
        RentedArrayType arrayType = RentedArrayType.None,
        bool isRented = false)
    {
        if (length < 0)
        {
            length = objLength;
        }

        if (arrayType != RentedArrayType.None) isRented = true;

        if (type == BufferType.Null)
        {
            Assert.That(buffer.Array, Is.Null);
            Assert.That(buffer.MemoryManager, Is.Null);
            Assert.That(buffer.MemoryOwner, Is.Null);
        }
        else if (type == BufferType.Array)
        {
            Assert.That(buffer.Array != null && buffer.Array.Length == objLength, Is.True);
            Assert.That(buffer.MemoryManager, Is.Null);
            Assert.That(buffer.MemoryOwner, Is.Null);
        }
        else if (type == BufferType.MemoryOwner)
        {
            Assert.That(buffer.Array, Is.Null);
            Assert.That(buffer.MemoryManager, Is.Null);

            if (buffer.IsRented)
            {
                Assert.That(buffer.MemoryOwner, Is.Not.Null);
                Assert.That(buffer.MemoryOwner.Memory.Length, Is.EqualTo(objLength));

                Assert.That(new Buffer<byte>(buffer.Memory).MemoryOwner, Is.Null);
            }
            else
            {
                Assert.That(buffer.MemoryOwner, Is.Null);
            }
        }
        else if (type == BufferType.MemoryManager)
        {
            Assert.That(buffer.Array, Is.Null);

            if (buffer.IsRented)
            {
                Assert.That(buffer.MemoryManager, Is.Not.Null);
                Assert.That(buffer.MemoryOwner, Is.Not.Null);

                Assert.That(buffer.MemoryManager.Memory.Length, Is.EqualTo(objLength));
                Assert.That(buffer.MemoryManager.GetSpan().Length, Is.EqualTo(objLength));
                Assert.That(buffer.MemoryOwner.Memory.Length, Is.EqualTo(objLength));

                Assert.That(new Buffer<byte>(buffer.Memory).MemoryManager, Is.Null);
                Assert.That(new Buffer<byte>(buffer.Memory).MemoryOwner, Is.Null);
            }
            else
            {
                Assert.That(buffer.MemoryManager, Is.Null);
                Assert.That(buffer.MemoryOwner, Is.Null);
            }
        }

        Assert.That(buffer.Type, Is.EqualTo(type));
        Assert.That(buffer.Start, Is.EqualTo(start));
        Assert.That(buffer.Length, Is.EqualTo(length));
        Assert.That(buffer.ArrayType, Is.EqualTo(arrayType));
        Assert.That(buffer.IsEmpty, Is.EqualTo(length == 0));
        Assert.That(buffer.IsRented, Is.EqualTo(isRented));

        Assert.That(buffer.Slice(0).IsRented, Is.EqualTo(isRented));
        Assert.That(buffer.Slice(0, 0).IsRented, Is.EqualTo(isRented));

        if (isRented)
        {
            EqualTo(buffer.AsUnrented(), type, objLength, start, length);
        }
        else
        {
            Assert.That(buffer.TryReturn(out _), Is.False);
            buffer.Return();
        }

        if (!buffer.IsEmpty)
        {
            EqualTo(buffer.AsEmpty(), type, objLength, buffer.Start, 0, arrayType: arrayType, isRented: isRented);
            Assert.That(buffer.AsEmpty(), Is.EqualTo(buffer.Slice(0, 0)));
        }
    }
}