using System.Diagnostics.CodeAnalysis;

namespace IT.Buffers;

public interface IBufferPool<TBuffer> : IBufferPool
{
    bool TryRent([MaybeNullWhen(false)] out TBuffer buffer);

    TBuffer Rent();

    bool TryReturn(TBuffer buffer);

    void Return(TBuffer buffer);
}