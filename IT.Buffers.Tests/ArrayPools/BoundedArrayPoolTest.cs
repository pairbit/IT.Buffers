using IT.Buffers.Internal;

namespace IT.Buffers.Tests;

internal class BoundedArrayPoolTest
{
    //[Test]
    public void Test()
    {
        var options = new BoundedArrayPoolOptions();
        var arrayPool = new BoundedArrayPool<byte>(options);

        for (int i = 0; i < 28; i++)
        {
            var length = xArray.GetMaxSizeForBucket(i) - 1;
            var array = arrayPool.Rent(length);
            Random.Shared.NextBytes(array);
            Assert.That(arrayPool.TryReturn(array), Is.True);

            var array2 = arrayPool.Rent(length);
            Assert.That(ReferenceEquals(array, array2), Is.True);

            Random.Shared.NextBytes(array2);
            Assert.That(arrayPool.TryReturn(array), Is.True);
        }
    }
}