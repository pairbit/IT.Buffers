namespace IT.Buffers.Interfaces;

public interface IBufferPool<TBuffer> : IBufferPool
{
    TBuffer Rent();

    bool TryReturn(TBuffer buffer);

    void Return(TBuffer buffer);
}