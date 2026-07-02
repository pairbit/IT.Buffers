using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers;

public sealed class ReadOnlySequenceStream : Stream
{
    private ReadOnlySequence<byte> _sequence;
    private SequencePosition _position;
    private long _absolutePosition;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlySequenceStream"/> class over the specified <see cref="ReadOnlySequence{Byte}"/>.
    /// </summary>
    /// <param name="source">The <see cref="ReadOnlySequence{Byte}"/> to wrap.</param>
    public ReadOnlySequenceStream(ReadOnlySequence<byte> sequence)
    {
        _sequence = sequence;
        _position = sequence.Start;
        _absolutePosition = 0;
        _isDisposed = false;
    }

    public override bool CanRead => !_isDisposed;

    public override bool CanSeek => !_isDisposed;

    public override bool CanWrite => false;

    private void EnsureNotDisposed()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(ReadOnlySequenceStream));
    }

    public override long Length
    {
        get
        {
            EnsureNotDisposed();
            return _sequence.Length;
        }
    }

    public override long Position
    {
        get
        {
            EnsureNotDisposed();
            return _absolutePosition;
        }
        set
        {
            EnsureNotDisposed();
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (value >= _sequence.Length)
            {
                _position = _sequence.End;
            }
            else if (value >= _absolutePosition)
            {
                _position = _sequence.GetPosition(value - _absolutePosition, _position);
            }
            else
            {
                _position = _sequence.GetPosition(value, _sequence.Start);
            }

            _absolutePosition = value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if ((uint)count > buffer.Length - offset) throw new ArgumentOutOfRangeException(nameof(count));

        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        EnsureNotDisposed();

        if (_absolutePosition >= _sequence.Length)
        {
            return 0;
        }

        ReadOnlySequence<byte> remaining = _sequence.Slice(_position);
        int n = (int)Math.Min(remaining.Length, buffer.Length);
        if (n <= 0)
        {
            return 0;
        }

        remaining.Slice(0, n).CopyTo(buffer);
        _position = _sequence.GetPosition(n, _position);
        _absolutePosition += n;
        return n;
    }

    public override int ReadByte()
    {
        EnsureNotDisposed();

        if (_absolutePosition >= _sequence.Length)
        {
            return 0;
        }

        ReadOnlySequence<byte> remaining = _sequence.Slice(_position);
        if (remaining.Length > 0)
        {
            byte by = remaining.First.Span[0];
            _position = _sequence.GetPosition(1, _position);
            _absolutePosition += 1;
            return by;
        }
        else
        {
            return -1;
        }
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if ((uint)count > buffer.Length - offset) throw new ArgumentOutOfRangeException(nameof(count));

        EnsureNotDisposed();

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }

        int n = Read(buffer, offset, count);
        return Task.FromResult(n);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        cancellationToken.ThrowIfCancellationRequested();
        
        return new ValueTask<int>(Read(buffer.Span));
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        CheckCopyToArguments(destination, bufferSize);
        EnsureNotDisposed();

        if (_absolutePosition >= _sequence.Length)
        {
            return;
        }

        ReadOnlySequence<byte> remaining = _sequence.Slice(_position);
        foreach (ReadOnlyMemory<byte> segment in remaining)
        {
            destination.Write(segment.Span);
        }

        _position = _sequence.End;
        _absolutePosition = _sequence.Length;
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        CheckCopyToArguments(destination, bufferSize);
        EnsureNotDisposed();

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_absolutePosition >= _sequence.Length)
        {
            return Task.CompletedTask;
        }

        return CopyToAsyncCore(destination, cancellationToken);
    }

    private async Task CopyToAsyncCore(Stream destination, CancellationToken cancellationToken)
    {
        ReadOnlySequence<byte> remaining = _sequence.Slice(_position);
        foreach (ReadOnlyMemory<byte> segment in remaining)
        {
            await destination.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
        }

        _position = _sequence.End;
        _absolutePosition = _sequence.Length;
    }

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("UnwritableStream");

    public override void Write(ReadOnlySpan<byte> buffer)
        => throw new NotSupportedException("UnwritableStream");

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => throw new NotSupportedException("UnwritableStream");

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("UnwritableStream");

    public override void SetLength(long value)
        => throw new NotSupportedException("UnwritableStream");

    public override long Seek(long offset, SeekOrigin origin)
    {
        EnsureNotDisposed();

        long basePosition = origin switch
        {
            SeekOrigin.Begin => 0L,
            SeekOrigin.Current => _absolutePosition,
            SeekOrigin.End => _sequence.Length,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (offset > long.MaxValue - basePosition)
            throw new ArgumentOutOfRangeException(nameof(offset));

        long absolutePosition = basePosition + offset;
        if (absolutePosition < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (absolutePosition >= _sequence.Length)
        {
            _position = _sequence.End;
        }
        else if (absolutePosition >= _absolutePosition)
        {
            _position = _sequence.GetPosition(absolutePosition - _absolutePosition, _position);
        }
        else
        {
            _position = _sequence.GetPosition(absolutePosition, _sequence.Start);
        }

        _absolutePosition = absolutePosition;

        return absolutePosition;
    }

    public override void Flush() { }

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

    protected override void Dispose(bool disposing)
    {
        _isDisposed = true;
        _sequence = default;
        base.Dispose(disposing);
    }

    private static void CheckCopyToArguments(Stream destination, int bufferSize)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
        if (!destination.CanWrite)
        {
            if (destination.CanRead)
                throw new NotSupportedException("UnwritableStream");

            throw new ObjectDisposedException(destination.GetType().Name);
        }
    }
}