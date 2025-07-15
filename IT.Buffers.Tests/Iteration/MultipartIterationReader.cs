using IT.Buffers.Tests.Internal.Iteration;

namespace IT.Buffers.Tests.Iteration;

internal ref struct MultipartIterationReader
{
    private readonly ReadOnlySpan<byte> _boundary;
    private readonly IReadOnlySpanList<byte> _spans;
    private long _offset;

    public readonly ReadOnlySpan<byte> Boundary => _boundary;

    public readonly long Offset => _offset;

    public MultipartIterationReader(ReadOnlySpan<byte> boundary, IReadOnlySpanList<byte> spans)
    {
        _boundary = boundary;
        _spans = spans;
    }
}