namespace IT.Buffers.Internal;

internal static class xArray
{
    public static T[] AllocateUninitialized<T>(int length)
    {
#if NET
        return typeof(T).IsPrimitive && typeof(T) != typeof(bool) ?
            System.GC.AllocateUninitializedArray<T>(length) :
            new T[length];
#else
        return new T[length];
#endif
    }
}