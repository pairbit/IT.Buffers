namespace IT.Buffers;

public class NewBufferPool<TBuffer> : BufferPool<TBuffer> where TBuffer : class, new()
{
    protected override TBuffer NewBuffer() => new();
}