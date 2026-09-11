using System.Diagnostics.CodeAnalysis;

namespace IT.Buffers;

public interface IBufferPool<TBuffer> : IBufferPool
{
    bool TryRent([MaybeNullWhen(false)] out TBuffer buffer);

    TBuffer Rent();

    bool TryReturn(TBuffer buffer, bool reset = true);

    void Return(TBuffer buffer, bool reset = true);
}