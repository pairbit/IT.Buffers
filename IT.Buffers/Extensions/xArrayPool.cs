using System.Buffers;
using System.Runtime.CompilerServices;

namespace IT.Buffers.Extensions;

public static class xArrayPool
{
    public static void ReturnAndClear<T>(this ArrayPool<T> pool, T[] array)
        => pool.Return(array, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
}