namespace IT.Buffers;

public class NewBufferPool<TBuffer> : BufferPool<TBuffer> where TBuffer : class, IResetable, new()
{
    private static readonly NewBufferPool<TBuffer> _shared = new();

    public static BufferPool<TBuffer> Shared => _shared;

    protected override TBuffer NewBuffer() => new();
}