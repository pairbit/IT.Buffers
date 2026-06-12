#if NET10_0_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;

namespace IT.Buffers.Tests;

internal class SharedArrayPoolTest
{
    [Test]
    public void Test()
    {
        //var shared = ArrayPool<byte>.Shared;


        Assert.That(SharedArrayPool<byte>.TrimCallbackCreated, Is.True);
    }

    static class SharedArrayPool<T>
    {
        private const string TypeName = "System.Buffers.SharedArrayPool`1[[!0]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";

        public static bool TrimCallbackCreated => get_trimCallbackCreated(ArrayPool<T>.Shared);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_trimCallbackCreated")]
        private static extern ref bool get_trimCallbackCreated([UnsafeAccessorType(TypeName)] object pool);
    }
}
#endif