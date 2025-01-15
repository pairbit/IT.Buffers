﻿using IT.Buffers.Extensions;
using IT.Buffers.Interfaces;
using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IT.Buffers;

public class LinkedBufferWriter<T> : IAdvancedBufferWriter<T>, IDisposable
{
    public static BufferPool<LinkedBufferWriter<T>> Pool =>
        BufferPool<LinkedBufferWriter<T>>.Shared;

    internal readonly List<BufferSegment<T>> _buffers;

    internal readonly T[] _firstBuffer;
    internal int _firstBufferWritten;

    internal BufferSegment<T> _current;
    private int _nextBufferSize;

    internal long _written;
    private int _writtenSegments;

    public int Written => checked((int)_written);

    public long WrittenLong => _written;

    public int WrittenSegments => _writtenSegments;

    private ReadOnlySpan<T> FirstBufferWrittenSpan
    {
        get
        {
            Debug.Assert(_firstBuffer.Length >= _firstBufferWritten);
            return new ReadOnlySpan<T>(_firstBuffer, 0, _firstBufferWritten);
        }
    }

    public LinkedBufferWriter()
    {
        _buffers = new List<BufferSegment<T>>();
        _firstBuffer = [];
        _firstBufferWritten = 0;
        _current = default;
        _nextBufferSize = 0;
        _written = 0;
    }

    public LinkedBufferWriter(int bufferSize, bool useFirstBuffer = false)
    {
        if (bufferSize < 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        _buffers = new List<BufferSegment<T>>();
        _firstBuffer = useFirstBuffer && bufferSize > 0 ? new T[bufferSize] : [];
        _firstBufferWritten = 0;
        _current = default;
        _nextBufferSize = bufferSize;
        _written = 0;
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public T[] DangerousGetFirstBuffer() => _firstBuffer;

    //public void ResetWritten()
    //{
    //    _written = 0;
    //    _firstBufferWritten = 0;
    //}

    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        if (sizeHint == 0) sizeHint = 1;

        if (_current.IsNull)
        {
            Debug.Assert(_firstBuffer.Length >= _firstBufferWritten);

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
            if (nextBufferSize == 0) _nextBufferSize = BufferSize.GetDoubleCapacity(next.Capacity);
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

    public bool TryWrite(Span<T> span)
    {
        var written = _written;
        if (span.Length < written) return false;

        if (written > 0)
        {
            var firstBufferWritten = _firstBufferWritten;
            if (firstBufferWritten > 0)
            {
                FirstBufferWrittenSpan.CopyTo(span);
                span = span[firstBufferWritten..];
            }

            if (_buffers.Count > 0)
            {
#if NET6_0_OR_GREATER
                foreach (ref var item in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_buffers))
#else
                foreach (var item in _buffers)
#endif
                {
                    Debug.Assert(item.Written > 0);
                    item.WrittenSpan.CopyTo(span);
                    span = span[item.Written..];
                }
            }

            if (!_current.IsNull)
            {
                Debug.Assert(_current.Written > 0);
                _current.WrittenSpan.CopyTo(span);
            }
        }

        return true;
    }

    public void Write<TBufferWriter>(ref TBufferWriter writer) where TBufferWriter : IBufferWriter<T>
    {
        if (_written == 0) return;

        if (_firstBufferWritten > 0)
            xBufferWriter.WriteSpan(ref writer, FirstBufferWrittenSpan);

        if (_buffers.Count > 0)
        {
#if NET6_0_OR_GREATER
            foreach (ref var item in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_buffers))
#else
            foreach (var item in _buffers)
#endif
            {
                Debug.Assert(item.Written > 0);
                xBufferWriter.WriteSpan(ref writer, item.WrittenSpan);
            }
        }

        if (!_current.IsNull)
        {
            Debug.Assert(_current.Written > 0);
            xBufferWriter.WriteSpan(ref writer, _current.WrittenSpan);
        }
    }

    public bool TryWriteAndReset(Span<T> span)
    {
        var written = _written;
        if (span.Length < written) return false;

        if (written > 0)
        {
            var firstBufferWritten = _firstBufferWritten;
            if (firstBufferWritten > 0)
            {
                FirstBufferWrittenSpan.CopyTo(span);
                span = span[firstBufferWritten..];
            }

            if (_buffers.Count > 0)
            {
#if NET6_0_OR_GREATER
                foreach (ref var item in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_buffers))
#else
                foreach (var item in _buffers)
#endif
                {
                    Debug.Assert(item.Written > 0);
                    item.WrittenSpan.CopyTo(span);
                    span = span[item.Written..];
                    item.Dispose();
                }
            }

            if (!_current.IsNull)
            {
                Debug.Assert(_current.Written > 0);
                _current.WrittenSpan.CopyTo(span);
                _current.Dispose();
            }

            ResetCore();
        }

        return true;
    }

    public void WriteAndReset<TBufferWriter>(ref TBufferWriter writer) where TBufferWriter : IBufferWriter<T>
    {
        if (_written == 0) return;

        if (_firstBufferWritten > 0)
            xBufferWriter.WriteSpan(ref writer, FirstBufferWrittenSpan);

        if (_buffers.Count > 0)
        {
#if NET6_0_OR_GREATER
            foreach (ref var item in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_buffers))
#else
            foreach (var item in _buffers)
#endif
            {
                Debug.Assert(item.Written > 0);
                xBufferWriter.WriteSpan(ref writer, item.WrittenSpan);
                item.Dispose(); // reset
            }
        }

        if (!_current.IsNull)
        {
            Debug.Assert(_current.Written > 0);
            xBufferWriter.WriteSpan(ref writer, _current.WrittenSpan);
            _current.Dispose();
        }

        ResetCore();
    }

    public Enumerator GetEnumerator() => new(this);

    public Memory<T> GetWrittenMemory(int segment = 0)
    {
        if (segment >= _writtenSegments) throw new ArgumentOutOfRangeException(nameof(segment));

        throw new NotImplementedException();
    }

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
            item.Dispose();
        }
        _current.Dispose();
        ResetCore();
    }

    // reset without list's BufferSegment element
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ResetCore()
    {
        _firstBufferWritten = 0;
        _buffers.Clear();
        _written = 0;
        _current = default;
        _nextBufferSize = 0;
    }

    void IDisposable.Dispose() => Reset();

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

                var firstBufferWritten = _parent._firstBufferWritten;
                if (firstBufferWritten > 0)
                {
                    Debug.Assert(_parent._firstBuffer.Length >= firstBufferWritten);
                    _current = _parent._firstBuffer.AsMemory(0, firstBufferWritten);
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
                if (_parent._current.Written > 0)
                {
                    _current = _parent._current.WrittenMemory;
                    return true;
                }
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