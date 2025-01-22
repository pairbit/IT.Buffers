using IT.Buffers.Interfaces;

namespace IT.Buffers.Extensions;

public static class xAdvancedBufferWriter
{
    public static T[] ToArray<TBufferWriter, T>(ref TBufferWriter writer)
        where TBufferWriter : IAdvancedBufferWriter<T>
    {
        var written = writer.Written;
        if (written == 0) return [];

        var array =
#if NET5_0_OR_GREATER
            System.GC.AllocateUninitializedArray<T>(written);
#else
            new T[written];
#endif

        writer.TryWrite(array);

        return array;
    }
}