using System.Buffers;

namespace IT.Buffers;

internal class HybridArrayPool<T> : ArrayPool<T>
{
    public override T[] Rent(int minimumLength)
    {
        throw new System.NotImplementedException();
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        throw new System.NotImplementedException();
    }
}