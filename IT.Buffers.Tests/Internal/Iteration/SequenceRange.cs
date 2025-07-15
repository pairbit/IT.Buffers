namespace IT.Buffers.Tests.Internal.Iteration;

//SequenceSegmentIndex
internal readonly struct SequenceRange
{
    private readonly int _startSegment;
    private readonly int _startIndex;
    private readonly int _endSegment;
    private readonly int _endIndex;

    public int StartSegment => _startSegment;

    public int StartIndex => _startIndex;

    public int EndSegment => _endSegment;

    public int EndIndex => _endIndex;

    public SequenceRange(int startSegment, int startIndex, int endSegment, int endIndex)
    {
        _startSegment = startSegment;
        _startIndex = startIndex;
        _endSegment = endSegment;
        _endIndex = endIndex;
    }
}