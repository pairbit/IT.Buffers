namespace IT.Buffers;

public readonly struct SequenceSegments<T>
{
    public RentableSequenceSegment<T> Start { get; }

    public RentableSequenceSegment<T> End { get; }

    public SequenceSegments(RentableSequenceSegment<T> start, RentableSequenceSegment<T> end)
    {
        Start = start;
        End = end;
    }
}