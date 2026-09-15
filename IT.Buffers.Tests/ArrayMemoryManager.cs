using System.Buffers;
using System.Runtime.InteropServices;

namespace IT.Buffers.Tests;

public sealed class ArrayMemoryManager<T> : MemoryManager<T> where T : struct
{
    private readonly T[] _array;
    private bool _isDisposed;

    public ArrayMemoryManager(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        _array = new T[length];
    }

    public override Span<T> GetSpan()
    {
        ThrowIfDisposed();
        unsafe
        {
            return new Span<T>(_array);
        }
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        ThrowIfDisposed();

        throw new NotSupportedException();
    }

    public override void Unpin()
    {

    }

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            Array.Clear(_array);
            _isDisposed = true;
        }
    }

    protected override bool TryGetArray(out ArraySegment<T> segment)
    {
        segment = new(_array);
        return true;
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().FullName);
    }
}