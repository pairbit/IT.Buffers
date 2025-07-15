namespace IT.Buffers.Tests.Internal.Iteration;

internal interface IReadOnlySpanList<T>
{
    public int Count { get; }

    ReadOnlySpan<T> this[int index] { get; }
}