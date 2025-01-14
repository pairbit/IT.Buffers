using IT.Buffers.Interfaces;

namespace IT.Buffers.Extensions;

public static class xAdvancedBufferWriter
{
    public static T[] ToArray<TBufferWriter, T>(ref TBufferWriter writer)
        where TBufferWriter : IAdvancedBufferWriter<T>
    {
        var written = writer.Written;
        if (written == 0) return [];

        var array = new T[written];

        writer.TryWrite(array);

        return array;
    }
}