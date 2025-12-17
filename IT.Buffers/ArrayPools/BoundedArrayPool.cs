using System;
using System.Buffers;

/*
namespace IT.Buffers;

public class BoundedArrayPool<T> : ArrayPool<T>
{
    private readonly BoundedConcurrentQueue<T[]> _queue;

    public BoundedArrayPool(int power2)
    {
        _queue = new(power2);
    }

    public RentedArray<T> RentArray(int minimumLength)
    {
        //_queue.TryDequeue(out array)

        throw new NotImplementedException();
    }

    public bool TryReturn(T[] array, bool clearArray = false)
    {
        return _queue.TryEnqueue(array);
    }

    #region ArrayPool

    public override T[] Rent(int minimumLength)
    {
        throw new NotImplementedException();
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        throw new NotImplementedException();
    }

    #endregion ArrayPool
}*/