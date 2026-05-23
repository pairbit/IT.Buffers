using IT.Buffers.Internal;

namespace IT.Buffers.Tests;

internal class xArrayTest
{
    [Test]
    public void SelectBucketIndexTest()
    {
        Assert.That(xArray.SelectBucketIndex(1), Is.EqualTo(0));
        Assert.That(xArray.SelectBucketIndex(15), Is.EqualTo(0));
        Assert.That(xArray.SelectBucketIndex(16), Is.EqualTo(0));
        Assert.That(xArray.SelectBucketIndex(17), Is.EqualTo(1));

        Assert.That(xArray.SelectBucketIndex(1024), Is.EqualTo(6));
        Assert.That(xArray.SelectBucketIndex(int.MaxValue), Is.EqualTo(27));
        Assert.That(xArray.SelectBucketIndex(BufferSize.Max), Is.EqualTo(27));
       
        Assert.That(xArray.SelectBucketIndex(0), Is.EqualTo(28));
        Assert.That(xArray.SelectBucketIndex(-1), Is.EqualTo(28));
    }

    [Test]
    public void GetMaxSizeForBucketTest()
    {
        Assert.That(xArray.GetMaxSizeForBucket(0), Is.EqualTo(16));
        Assert.That(xArray.GetMaxSizeForBucket(1), Is.EqualTo(32));
        Assert.That(xArray.GetMaxSizeForBucket(2), Is.EqualTo(64));
        Assert.That(xArray.GetMaxSizeForBucket(3), Is.EqualTo(128));
        Assert.That(xArray.GetMaxSizeForBucket(26), Is.EqualTo(1073741824));
    }
}