using System;

namespace IT.Buffers.Internal;

internal static class xArray
{
    public static T[] AllocateUninitialized<T>(int length)
    {
#if NET
        return typeof(T).IsPrimitive && typeof(T) != typeof(bool) ?
            GC.AllocateUninitializedArray<T>(length) :
            new T[length];
#else
        return new T[length];
#endif
    }

    public static void Clear(this Array array)
    {
#if NET
        Array.Clear(array);
#else
        Array.Clear(array, 0, array.Length);
#endif
    }
}