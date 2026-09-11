namespace IT.Buffers.Interfaces;

public interface IRentedBuffer<TBuffer>
{
    IBufferPool<TBuffer>? BufferPool { get; }

    public void SetBufferPool(IBufferPool<TBuffer> bufferPool);
}