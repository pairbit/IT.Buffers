//using IT.Buffers.Internal;
//using System.Buffers;
//using System.Diagnostics.CodeAnalysis;

//namespace IT.Buffers;

//public class BoundedArrayPool<T>
//{
//    private readonly BoundedConcurrentQueue<T[]> _queue;

//    public BoundedArrayPool(int power2)
//    {
//        _queue = new(power2);
//    }

//    public bool TryRent([MaybeNullWhen(false)] out T[] array)
//        => _queue.TryDequeue(out array);

//    public bool TryReturn(T[] array, bool clearArray = false)
//    {
//        return _queue.TryEnqueue(array);
//    }
//}