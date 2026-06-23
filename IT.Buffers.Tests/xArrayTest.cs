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
        Assert.That(xArray.SelectBucketIndex(int.MinValue + 1), Is.EqualTo(28));
        Assert.That(xArray.SelectBucketIndex(int.MinValue), Is.EqualTo(27));

        var indexes = new List<int>();
        var bufferSize = BufferSize.KB_32;
        float bufferGrowthFactor = 1.19f;//1.19 or 1.4
        int maxBufferSize = BufferSize.Max;
        //var size = BufferSize.Max;

        do
        {
            //size -= bufferSize;
            indexes.Add(xArray.SelectBucketIndex(bufferSize));

            var newSize = (int)Math.Floor(bufferSize * bufferGrowthFactor);
            if ((uint)newSize > maxBufferSize)
                newSize = maxBufferSize;

            bufferSize = newSize;
        } while (bufferSize < maxBufferSize);
    }

    [Test]
    public void GetMaxSizeForBucketTest()
    {
        Assert.That(xArray.GetMaxSizeForBucket(0), Is.EqualTo(16));
        Assert.That(xArray.GetMaxSizeForBucket(1), Is.EqualTo(32));
        Assert.That(xArray.GetMaxSizeForBucket(2), Is.EqualTo(64));
        Assert.That(xArray.GetMaxSizeForBucket(3), Is.EqualTo(128));
        Assert.That(xArray.GetMaxSizeForBucket(26), Is.EqualTo(1073741824));
        Assert.That(xArray.GetMaxSizeForBucket(27), Is.EqualTo(Array.MaxLength));
    }
}