using IT.Buffers.Tests.Internal.Iteration;

namespace IT.Buffers.Tests.Iteration;

internal abstract class ReadOnlySpanIteratorTest<TIterator, T>
    where TIterator : IReadOnlySpanIterator<T>
{
    [Test]
    public void Iterate()
    {
        IterateAll(GetIterator());
    }

    protected abstract void Iterate(ReadOnlySpan<T> span, int index);

    protected abstract TIterator GetIterator();

    private void IterateAll(TIterator iterator)
    {
        var i = 0;
        while (iterator.TryGetNextSpan(out var span))
        {
            Iterate(span, i++);
        }
    }
}