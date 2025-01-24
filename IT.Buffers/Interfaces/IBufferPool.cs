namespace IT.Buffers.Interfaces;

public interface IBufferPool<TBuffer>
{
    TBuffer Rent();

    bool Return(TBuffer buffer);
}