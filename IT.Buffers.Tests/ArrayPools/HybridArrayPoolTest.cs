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

    //[Test]
    public void Test_3()
    {
        var options = new HybridArrayPoolOptions(27);
        BacketsTest(options.SetPow2(31), 27);
    }

    //[Test]
    public void Test_4()
    {
        var options = HybridArrayPoolOptions.CreateShared();
        BacketsTest(options);
    }

    //[Test]
    public void Test_5()
    {
        var options = Create5();
        BacketsTest(options);
    }

    private static void BacketsTest(HybridArrayPoolOptions options, int lastIndex = HybridArrayPoolOptions.MaxLength)
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
            Assert.That(ReferenceEquals(array, array2), Is.EqualTo(i < lastIndex));

            Random.Shared.NextBytes(array2);
            arrayPool.Return(array);
        }

        arrayPool.Clear();
    }

    private const byte Pow2_Shared = 31;

    private static HybridArrayPoolOptions Create5() => new([
        Pow2_Shared,//16
        Pow2_Shared,//32
        Pow2_Shared,//64
        Pow2_Shared,//128
        Pow2_Shared,//256
        Pow2_Shared,//512
        Pow2_Shared,//1KB
        Pow2_Shared,//2KB
        Pow2_Shared,//4KB
        Pow2_Shared,//8KB
        Pow2_Shared,//16KB
        Pow2_Shared,//32KB
        Pow2_Shared,//64KB
        Pow2_Shared,//128KB
        Pow2_Shared,//256KB
        Pow2_Shared,//512KB
        Pow2_Shared,//1MB
        Pow2_Shared,//2MB
        Pow2_Shared,//4MB
        Pow2_Shared,//8MB
        Pow2_Shared,//16MB
        Pow2_Shared,//32MB
        Pow2_Shared,//64MB
        Pow2_Shared,//128MB
        Pow2_Shared,//256MB
        Pow2_Shared,//512MB
        0,//1GB
        0//MAX
    ]);
}