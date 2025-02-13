using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers;

public class BufferWriterStream : Stream
{
    private readonly IBufferWriter<byte> _writer;
    private bool _isDisposed;

    public BufferWriterStream(IBufferWriter<byte> writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => !_isDisposed;

    public override long Length => throw ThrowDisposedOrNotSupported();

    public override long Position
    {
        get => throw ThrowDisposedOrNotSupported();
        set => ThrowDisposedOrNotSupported();
    }

    public override void Flush()
    {
        _isDisposed = true;
    }

    public override Task FlushAsync(CancellationToken cancellationToken) => ReturnOrThrowDisposed(Task.CompletedTask);

    public override int Read(byte[] buffer, int offset, int count) => throw ThrowDisposedOrNotSupported();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw ThrowDisposedOrNotSupported();

    public override int ReadByte() => throw ThrowDisposedOrNotSupported();

    public override long Seek(long offset, SeekOrigin origin) => throw ThrowDisposedOrNotSupported();

    public override void SetLength(long value) => ThrowDisposedOrNotSupported();

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (_isDisposed) throw new ObjectDisposedException(nameof(BufferWriterStream));

        Span<byte> span = _writer.GetSpan(count);

        buffer.AsSpan(offset, count).CopyTo(span);

        _writer.Advance(count);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Write(buffer, offset, count);

        return Task.CompletedTask;
    }

    public override void WriteByte(byte value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(BufferWriterStream));

        Span<byte> span = _writer.GetSpan(1);

        span[0] = value;

        _writer.Advance(1);
    }

    public override int Read(Span<byte> buffer) => throw ThrowDisposedOrNotSupported();

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => throw ThrowDisposedOrNotSupported();

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(BufferWriterStream));

        Span<byte> span = _writer.GetSpan(buffer.Length);

        buffer.CopyTo(span);

        _writer.Advance(buffer.Length);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Write(buffer.Span);

        return default;
    }

    protected override void Dispose(bool disposing)
    {
        _isDisposed = true;
        base.Dispose(disposing);
    }

    private T ReturnOrThrowDisposed<T>(T value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(BufferWriterStream));
        return value;
    }

    private Exception ThrowDisposedOrNotSupported()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(BufferWriterStream));
        throw new NotSupportedException();
    }
}