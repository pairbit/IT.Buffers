using IT.Buffers.Interfaces;

namespace IT.Buffers.Extensions;

public static class xAdvancedBufferWriter
{
    public static T[] ToArray<TBufferWriter, T>(ref TBufferWriter writer)
        where TBufferWriter : IAdvancedBufferWriter<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
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