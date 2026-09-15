using System;
using System.Buffers;

namespace IT.Buffers;

internal abstract class SequenceSegmentDisposable<T> : SequenceSegment<T>, IMemoryOwner<T>
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}