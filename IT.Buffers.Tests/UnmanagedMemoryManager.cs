using System.Buffers;
using System.Runtime.InteropServices;

namespace IT.Buffers.Tests;

public sealed class UnmanagedMemoryManager<T> : MemoryManager<T> where T : struct
{
    private readonly IntPtr _pointer;
    private readonly int _length;
    private bool _isDisposed;

    public UnmanagedMemoryManager(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        _length = length;
        int byteSize = length * Marshal.SizeOf<T>();
        _pointer = Marshal.AllocHGlobal(byteSize);
    }

    // Возвращает Span<T>, указывающий на наш неуправляемый буфер
    public override Span<T> GetSpan()
    {
        ThrowIfDisposed();
        unsafe
        {
            return new Span<T>((void*)_pointer, _length);
        }
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        ThrowIfDisposed();
        if (elementIndex < 0 || elementIndex >= _length)
            throw new ArgumentOutOfRangeException(nameof(elementIndex));

        unsafe
        {
            void* p = (void*)(_pointer + elementIndex * Marshal.SizeOf<T>());
            return new MemoryHandle(p);
        }
    }

    public override void Unpin()
    {

    }

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            Marshal.FreeHGlobal(_pointer);
            _isDisposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().FullName);
    }
}