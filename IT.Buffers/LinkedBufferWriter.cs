using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers;

public class LinkedBufferWriter : IBufferWriter<byte>
{
    const int InitialBufferSize = 262144; // 256K(32768, 65536, 131072, 262144)
    private static readonly byte[] _noUseFirstBufferSentinel = new byte[0];

    private List<BufferSegment> _buffers; // add freezed _buffer.

    private byte[] _firstBuffer; // cache _firstBuffer to avoid call ArrayPoo.Rent/Return
    private int _firstBufferWritten;

    private BufferSegment _current;
    private int _nextBufferSize;

    private long _written;

    public long WrittenCount => _written;

    private bool UseFirstBuffer => _firstBuffer != _noUseFirstBufferSentinel;

    public LinkedBufferWriter(bool useFirstBuffer)
    {
        _buffers = new List<BufferSegment>();
        _firstBuffer = useFirstBuffer ? new byte[InitialBufferSize] : _noUseFirstBufferSentinel;
        _firstBufferWritten = 0;
        _current = default;
        _nextBufferSize = InitialBufferSize;
        _written = 0;
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public byte[] DangerousGetFirstBuffer() => _firstBuffer;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_current.IsNull)
        {
            // use _firstBuffer
            var free = _firstBuffer.Length - _firstBufferWritten;
            if (free != 0 && sizeHint <= free)
            {
                return _firstBuffer.AsMemory(_firstBufferWritten);
            }
        }
        else
        {
            var buffer = _current.FreeMemory;
            if (buffer.Length > sizeHint) return buffer;
        }

        BufferSegment next;
        if (sizeHint <= _nextBufferSize)
        {
            next = new BufferSegment(_nextBufferSize);
            _nextBufferSize = BufferSize.GetDoubleCapacity(_nextBufferSize);
        }
        else
        {
            next = new BufferSegment(sizeHint);
        }

        if (_current.WrittenCount != 0)
        {
            _buffers.Add(_current);
        }
        _current = next;
        return next.FreeMemory;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (_current.IsNull)
        {
            // use _firstBuffer
            var free = _firstBuffer.Length - _firstBufferWritten;
            if (free != 0 && sizeHint <= free)
            {
                return _firstBuffer.AsSpan(_firstBufferWritten);
            }
        }
        else
        {
            var buffer = _current.FreeSpan;
            if (buffer.Length > sizeHint) return buffer;
        }

        BufferSegment next;
        if (sizeHint <= _nextBufferSize)
        {
            next = new BufferSegment(_nextBufferSize);
            _nextBufferSize = BufferSize.GetDoubleCapacity(_nextBufferSize);
        }
        else
        {
            next = new BufferSegment(sizeHint);
        }

        if (_current.WrittenCount != 0)
        {
            _buffers.Add(_current);
        }
        _current = next;
        return next.FreeSpan;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

        if (_current.IsNull)
        {
            var firstBufferWritten = _firstBufferWritten + count;
            if (firstBufferWritten > _firstBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            _firstBufferWritten = firstBufferWritten;
        }
        else
        {
            _current.Advance(count);
        }
        _written += count;
    }

    public byte[] ToArrayAndReset()
    {
        if (_written == 0) return Array.Empty<byte>();

        var result = new byte[_written];
        var dest = result.AsSpan();

        if (UseFirstBuffer)
        {
            _firstBuffer.AsSpan(0, _firstBufferWritten).CopyTo(dest);
            dest = dest.Slice(_firstBufferWritten);
        }

        if (_buffers.Count > 0)
        {
#if NET6_0_OR_GREATER
            foreach (ref var item in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_buffers))
#else
            foreach (var item in _buffers)
#endif
            {
                item.WrittenSpan.CopyTo(dest);
                dest = dest.Slice(item.WrittenCount);
                item.Clear(); // reset _buffer-segment in this loop to avoid iterate twice for Reset
            }
        }

        if (!_current.IsNull)
        {
            _current.WrittenSpan.CopyTo(dest);
            _current.Clear();
        }

        ResetCore();
        return result;
    }

    public void WriteToAndReset<TBufferWriter>(in TBufferWriter writer)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (_written == 0) return;

        if (UseFirstBuffer)
        {
            var written = _firstBufferWritten;

            var span = writer.GetSpan(written);

            _firstBuffer.AsSpan(0, written).CopyTo(span[..written]);

            writer.Advance(written);
        }

        if (_buffers.Count > 0)
        {
#if NET6_0_OR_GREATER
            foreach (ref var item in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_buffers))
#else
            foreach (var item in _buffers)
#endif
            {
                var written = item.WrittenCount;

                var span = writer.GetSpan(written);

                item.WrittenSpan.CopyTo(span[..written]);

                writer.Advance(written);

                item.Clear(); // reset
            }
        }

        if (!_current.IsNull)
        {
            var written = _current.WrittenCount;

            var span = writer.GetSpan(written);

            _current.WrittenSpan.CopyTo(span[..written]);

            writer.Advance(_current.WrittenCount);

            _current.Clear();
        }

        ResetCore();
    }

    public async ValueTask WriteToAndResetAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (_written == 0) return;

        if (UseFirstBuffer)
        {
            await stream.WriteAsync(_firstBuffer.AsMemory(0, _firstBufferWritten), cancellationToken).ConfigureAwait(false);
        }

        if (_buffers.Count > 0)
        {
            foreach (var item in _buffers)
            {
                await stream.WriteAsync(item.WrittenMemory, cancellationToken).ConfigureAwait(false);
                item.Clear(); // reset
            }
        }

        if (!_current.IsNull)
        {
            await stream.WriteAsync(_current.WrittenMemory, cancellationToken).ConfigureAwait(false);
            _current.Clear();
        }

        ResetCore();
    }

    public Enumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        if (_written == 0) return;
#if NET6_0_OR_GREATER
        foreach (ref var item in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_buffers))
#else
        foreach (var item in _buffers)
#endif
        {
            item.Clear();
        }
        _current.Clear();
        ResetCore();
    }

    // reset without list's BufferSegment element
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResetCore()
    {
        _firstBufferWritten = 0;
        _buffers.Clear();
        _written = 0;
        _current = default;
        _nextBufferSize = InitialBufferSize;
    }

    public struct Enumerator : IEnumerator<Memory<byte>>
    {
        private readonly LinkedBufferWriter _parent;
        private State _state;
        private Memory<byte> _current;
        private List<BufferSegment>.Enumerator _buffersEnumerator;

        public Enumerator(LinkedBufferWriter parent)
        {
            _parent = parent;
            _state = default;
            _current = default;
            _buffersEnumerator = default;
        }

        public readonly Memory<byte> Current => _current;

        readonly object IEnumerator.Current => throw new NotSupportedException();

        public readonly void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_state == State.FirstBuffer)
            {
                _state = State.BuffersInit;

                if (_parent.UseFirstBuffer)
                {
                    _current = _parent._firstBuffer.AsMemory(0, _parent._firstBufferWritten);
                    return true;
                }
            }

            if (_state == State.BuffersInit)
            {
                _state = State.BuffersIterate;

                _buffersEnumerator = _parent._buffers.GetEnumerator();
            }

            if (_state == State.BuffersIterate)
            {
                if (_buffersEnumerator.MoveNext())
                {
                    _current = _buffersEnumerator.Current.WrittenMemory;
                    return true;
                }

                _buffersEnumerator.Dispose();
                _state = State.Current;
            }

            if (_state == State.Current)
            {
                _state = State.End;

                _current = _parent._current.WrittenMemory;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _state = default;
            _current = default;
            _buffersEnumerator = default;
        }

        enum State
        {
            FirstBuffer,
            BuffersInit,
            BuffersIterate,
            Current,
            End
        }
    }
}