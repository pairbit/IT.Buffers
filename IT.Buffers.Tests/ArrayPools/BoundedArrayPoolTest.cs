using IT.Buffers.Internal;

namespace IT.Buffers.Tests;

internal class BoundedArrayPoolTest
{
    [Test]
    public void Invalid()
    {
        var options = new BoundedArrayPoolOptions();
        var arrayPool = new BoundedArrayPool<byte>(options);

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => arrayPool.Rent(int.MaxValue))
            .ParamName, Is.EqualTo("minimumLength"));

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => arrayPool.Rent(int.MinValue))
            .ParamName, Is.EqualTo("minimumLength"));
    }

    //[Test]
    public void Test()
    {
        var options = new BoundedArrayPoolOptions();
        var arrayPool = new BoundedArrayPool<byte>(options);

        var array = arrayPool.Rent(0);
        Assert.That(array.Length == 0, Is.True);
        Assert.That(arrayPool.TryReturn(array), Is.False);

        for (int i = 0; i < 28; i++)
        {
            var length = xArray.GetMaxSizeForBucket(i) - 1;
            array = arrayPool.Rent(length);
            Random.Shared.NextBytes(array);
            Assert.That(arrayPool.TryReturn(array), Is.True);

            var array2 = arrayPool.Rent(length);
            Assert.That(ReferenceEquals(array, array2), Is.True);

            Random.Shared.NextBytes(array2);
            Assert.That(arrayPool.TryReturn(array), Is.True);
        }

        arrayPool.Clear();
    }
}