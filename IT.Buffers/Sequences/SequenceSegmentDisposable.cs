using System;
using System.Buffers;
using System.Threading;

namespace IT.Buffers;

internal class SequenceSegmentDisposable<T> : SequenceSegment<T>, IMemoryOwner<T>
{
    private object? _buffer;

    public SequenceSegmentDisposable(T[] array)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));

        _buffer = array;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            var buffer = Interlocked.Exchange(ref _buffer, null);
            if (buffer != null)
            {

            }
        }
    }
}