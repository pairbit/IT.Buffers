using System.Buffers;

namespace IT.Buffers;

internal abstract class AdvancedArrayPool<T> : ArrayPool<T>
{
    public abstract bool TryRent(int minimumLength, out T[] array);

    public abstract bool TryReturn(T[] array, bool clearArray = false);
}