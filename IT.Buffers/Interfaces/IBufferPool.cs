namespace IT.Buffers.Interfaces;

public interface IBufferPool<TBuffer>
{
    TBuffer Rent();

    void Return(TBuffer buffer);
}