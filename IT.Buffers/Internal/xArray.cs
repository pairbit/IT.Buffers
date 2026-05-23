using System;
using System.Runtime.CompilerServices;

namespace IT.Buffers.Internal;

internal static class xArray
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SelectBucketIndex(int bufferSize)
    {
        // Buffers are bucketed so that a request between 2^(n-1) + 1 and 2^n is given a buffer of 2^n
        // Bucket index is log2(bufferSize - 1) with the exception that buffers between 1 and 16 bytes
        // are combined, and the index is slid down by 3 to compensate.
        // Zero is a valid bufferSize, and it is assigned the highest bucket index so that zero-length
        // buffers are not retained by the pool. The pool will return the Array.Empty singleton for these.
        return System.Numerics.BitOperations.Log2((uint)bufferSize - 1 | 15) - 3;
    }

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