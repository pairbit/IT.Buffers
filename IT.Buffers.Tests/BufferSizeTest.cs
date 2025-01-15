namespace IT.Buffers.Tests;

public class BufferSizeTest
{
    [Test]
    public void Test()
    {
        Assert.That(BufferSize<char>.KB_256, Is.EqualTo(BufferSize.KB_128));
        Assert.That(BufferSize<int>.KB_256, Is.EqualTo(BufferSize.KB_64));
        Assert.That(BufferSize<long>.KB_256, Is.EqualTo(BufferSize.KB_32));
        Assert.That(BufferSize<Guid>.KB_256, Is.EqualTo(BufferSize.KB_16));
    }
}