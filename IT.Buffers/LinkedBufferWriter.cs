using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IT.Buffers;

public class LinkedBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    //const int InitialBufferSize = 262144; // 256K(32768, 65536, 131072, 262144)
    private static readonly T[] _noUseFirstBufferSentinel = new T[0];

    private readonly List<BufferSegment<T>> _buffers;

    private readonly T[] _firstBuffer;
    private int _firstBufferWritten;

    private BufferSegment<T> _current;
    private int _nextBufferSize;

    private long _written;

    public int Written => checked((int)_written);

    public long WrittenLong => _written;

    private bool UseFirstBuffer => _firstBuffer != _noUseFirstBufferSentinel;

    public LinkedBufferWriter(int bufferSize = BufferSize.KB_256, bool useFirstBuffer = false)
    {
        _buffers = new List<BufferSegment<T>>();
        _firstBuffer = useFirstBuffer ? new T[bufferSize] : _noUseFirstBufferSentinel;
        _firstBufferWritten = 0;
        _current = default;
        _nextBufferSize = bufferSize;
        _written = 0;
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public T[] DangerousGetFirstBuffer() => _firstBuffer;

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        if (sizeHint == 0) sizeHint = 1;

        if (_current.IsNull)
        {
            var free = _firstBuffer.Length - _firstBufferWritten;
            if (free >= sizeHint) return _firstBuffer.AsMemory(_firstBufferWritten);
        }
        else
        {
            var freeMemory = _current.FreeMemory;
            if (freeMemory.Length >= sizeHint) return freeMemory;
        }

        BufferSegment<T> next;
        var nextBufferSize = _nextBufferSize;
        if (nextBufferSize >= sizeHint)
        {
            next = new BufferSegment<T>(nextBufferSize);
            _nextBufferSize = BufferSize.GetDoubleCapacity(next.Capacity);
        }
        else
        {
            next = new BufferSegment<T>(sizeHint);
            if (nextBufferSize == 0) _nextBufferSize = next.Capacity;
        }

        if (_current.Written != 0)
        {
            _buffers.Add(_current);
        }
        _current = next;
        return next.FreeMemory;
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        if (sizeHint == 0) sizeHint = 1;

        if (_current.IsNull)
        {
            Debug.Assert(_firstBuffer.Length >= _firstBufferWritten);

            var freeCapacity = _firstBuffer.Length - _firstBufferWritten;
            if (freeCapacity >= sizeHint) return _firstBuffer.AsSpan(_firstBufferWritten);
        }
        else
        {
            var freeSpan = _current.FreeSpan;
            if (freeSpan.Length >= sizeHint) return freeSpan;
        }

        BufferSegment<T> next;
        var nextBufferSize = _nextBufferSize;
        if (nextBufferSize >= sizeHint)
        {
            next = new BufferSegment<T>(nextBufferSize);
            _nextBufferSize = BufferSize.GetDoubleCapacity(next.Capacity);
        }
        else
        {
            next = new BufferSegment<T>(sizeHint);
            if (nextBufferSize == 0) _nextBufferSize = next.Capacity;
        }

        if (_current.Written != 0)
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

    public T[] ToArrayAndDispose()
    {
        if (_written == 0) return Array.Empty<T>();

        var result = new T[_written];
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
                dest = dest.Slice(item.Written);
                item.Dispose(); // reset _buffer-segment in this loop to avoid iterate twice for Reset
            }
        }

        if (!_current.IsNull)
        {
            _current.WrittenSpan.CopyTo(dest);
            _current.Dispose();
        }

        ResetCore();
        return result;
    }

    public void WriteAndDispose<TBufferWriter>(in TBufferWriter writer)
        where TBufferWriter : IBufferWriter<T>
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
                var written = item.Written;

                var span = writer.GetSpan(written);

                item.WrittenSpan.CopyTo(span[..written]);

                writer.Advance(written);

                item.Dispose(); // reset
            }
        }

        if (!_current.IsNull)
        {
            var written = _current.Written;

            var span = writer.GetSpan(written);

            _current.WrittenSpan.CopyTo(span[..written]);

            writer.Advance(_current.Written);

            _current.Dispose();
        }

        ResetCore();
    }

    /*
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
                item.Dispose(); // reset
            }
        }

        if (!_current.IsNull)
        {
            await stream.WriteAsync(_current.WrittenMemory, cancellationToken).ConfigureAwait(false);
            _current.Dispose();
        }

        ResetCore();
    }
    */

    public Enumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_written == 0) return;
#if NET6_0_OR_GREATER
        foreach (ref var item in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_buffers))
#else
        foreach (var item in _buffers)
#endif
        {
            item.Dispose();
        }
        _current.Dispose();
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
        _nextBufferSize = 0;
    }

    public struct Enumerator : IEnumerator<Memory<T>>
    {
        private readonly LinkedBufferWriter<T> _parent;
        private State _state;
        private Memory<T> _current;
        private List<BufferSegment<T>>.Enumerator _buffersEnumerator;

        public Enumerator(LinkedBufferWriter<T> parent)
        {
            _parent = parent;
            _state = default;
            _current = default;
            _buffersEnumerator = default;
        }

        public readonly Memory<T> Current => _current;

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