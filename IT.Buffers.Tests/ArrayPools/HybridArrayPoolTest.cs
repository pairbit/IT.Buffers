using IT.Buffers.Internal;

namespace IT.Buffers.Tests;

internal class HybridArrayPoolTest
{
    [Test]
    public void Invalid()
    {
        var options = new HybridArrayPoolOptions();
        var arrayPool = new HybridArrayPool<byte>(options);

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => arrayPool.Rent(int.MaxValue))
            .ParamName, Is.EqualTo("minimumLength"));

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => arrayPool.Rent(int.MinValue))
            .ParamName, Is.EqualTo("minimumLength"));
    }

    //[Test]
    public void Test_1()
    {
        var options = new HybridArrayPoolOptions();
        BacketsTest(options);
    }

    //[Test]
    public void Test_2()
    {
        var options = HybridArrayPoolOptions.Create();
        BacketsTest(options);
    }

    private static void BacketsTest(HybridArrayPoolOptions options)
    {
        var arrayPool = new HybridArrayPool<byte>(options);

        var array = arrayPool.Rent(0);
        Assert.That(array.Length == 0, Is.True);
        arrayPool.Return(array);

        for (int i = 0; i < 28; i++)
        {
            var length = xArray.GetMaxSizeForBucket(i) - 1;
            array = arrayPool.Rent(length);
            Random.Shared.NextBytes(array);
            arrayPool.Return(array);

            var array2 = arrayPool.Rent(length);
            Assert.That(ReferenceEquals(array, array2), Is.True);

            Random.Shared.NextBytes(array2);
            arrayPool.Return(array);
        }

        arrayPool.Clear();
    }
}