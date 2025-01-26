namespace IT.Buffers.Interfaces;

public interface IBufferPool<TBuffer>
{
    TBuffer Rent();

    bool TryReturn(TBuffer buffer);
}