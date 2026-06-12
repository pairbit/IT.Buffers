#if NET10_0_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;

namespace IT.Buffers.Tests;

internal class SharedArrayPoolTest
{
    //[Test]
    public void Test()
    {
        var shared = ArrayPool<byte>.Shared;

        var r1 = shared.Rent(8 * 1024);
        var r2 = shared.Rent(8 * 1024);

        shared.Return(r1);
        shared.Return(r2);



        Assert.That(SharedArrayPool<byte>.TrimCallbackCreated, Is.True);
    }

    static class SharedArrayPool<T>
    {
        private const string TypeName = "System.Buffers.SharedArrayPool`1[[!0]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";

        public static bool TrimCallbackCreated => get_trimCallbackCreated(ArrayPool<T>.Shared);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_trimCallbackCreated")]
        private static extern ref bool get_trimCallbackCreated([UnsafeAccessorType(TypeName)] object pool);
    }

    static class SharedArrayPoolStatics
    {
        internal static readonly int s_partitionCount = GetPartitionCount();

        internal static readonly int s_maxArraysPerPartition = GetMaxArraysPerPartition();

        internal static int GetPartitionIndex() => (int)((uint)Thread.GetCurrentProcessorId() % (uint)s_partitionCount);

        private static int GetPartitionCount()
        {
            int partitionCount = int.TryParse("DOTNET_SYSTEM_BUFFERS_SHAREDARRAYPOOL_MAXPARTITIONCOUNT", out int result) && result > 0 ?
                result :
                int.MaxValue;

            return Math.Min(partitionCount, Environment.ProcessorCount);
        }

        private static int GetMaxArraysPerPartition()
        {
            return int.TryParse("DOTNET_SYSTEM_BUFFERS_SHAREDARRAYPOOL_MAXARRAYSPERPARTITION", out int result) && result > 0 ?
                result :
                32;
        }
    }
}
#endif