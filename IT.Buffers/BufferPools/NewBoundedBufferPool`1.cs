namespace IT.Buffers;

public class NewBoundedBufferPool<TBuffer> : BoundedBufferPool<TBuffer> where TBuffer : class, new()
{
    public NewBoundedBufferPool(int id, int pow2 = 5) : base(id, pow2)
    {
    }

    protected override TBuffer NewBuffer() => new();
}