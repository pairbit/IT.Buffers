namespace IT.Buffers.Tests.Internal.Iteration;

internal readonly struct RangeLong
{
    private readonly long _start;
    private readonly long _end;

    public long Start => _start;

    public long End => _end;

    public RangeLong(long start, long end)
    {
        _start = start;
        _end = end;
    }
}