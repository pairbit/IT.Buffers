namespace IT.Buffers;

public readonly struct SequenceSegments<T>
{
    public SequenceSegment<T> Start { get; }

    public SequenceSegment<T> End { get; }

    public SequenceSegments(SequenceSegment<T> start, SequenceSegment<T> end)
    {
        Start = start;
        End = end;
    }
}