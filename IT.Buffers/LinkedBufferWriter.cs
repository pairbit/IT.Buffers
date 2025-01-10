using IT.Buffers.Internal;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace IT.Buffers;

public class LinkedBufferWriter : IBufferWriter<byte>
{
    const int InitialBufferSize = 262144; // 256K(32768, 65536, 131072, 262144)
    static readonly byte[] noUseFirstBufferSentinel = new byte[0];

    List<BufferSegment> buffers; // add freezed _buffer.

    byte[] firstBuffer; // cache firstBuffer to avoid call ArrayPoo.Rent/Return
    int firstBufferWritten;

    BufferSegment current;
    int nextBufferSize;

    int totalWritten;

    public int TotalWritten => totalWritten;
    bool UseFirstBuffer => firstBuffer != noUseFirstBufferSentinel;

    public LinkedBufferWriter(bool useFirstBuffer)
    {
        this.buffers = new List<BufferSegment>();
        this.firstBuffer = useFirstBuffer
            ? new byte[InitialBufferSize]
            : noUseFirstBufferSentinel;
        this.firstBufferWritten = 0;
        this.current = default;
        this.nextBufferSize = InitialBufferSize;
        this.totalWritten = 0;
    }

    public byte[] DangerousGetFirstBuffer() => firstBuffer;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        // MemoryPack don't use GetMemory.
        throw new NotSupportedException();
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (current.IsNull)
        {
            // use firstBuffer
            var free = firstBuffer.Length - firstBufferWritten;
            if (free != 0 && sizeHint <= free)
            {
                return firstBuffer.AsSpan(firstBufferWritten);
            }
        }
        else
        {
            var buffer = current.FreeSpan;
            if (buffer.Length > sizeHint)
            {
                return buffer;
            }
        }

        BufferSegment next;
        if (sizeHint <= nextBufferSize)
        {
            next = new BufferSegment(nextBufferSize);
            nextBufferSize = NewArrayCapacity(nextBufferSize);
        }
        else
        {
            next = new BufferSegment(sizeHint);
        }

        if (current.WrittenCount != 0)
        {
            buffers.Add(current);
        }
        current = next;
        return next.FreeSpan;
    }

    private static int NewArrayCapacity(int size)
    {
        var newSize = unchecked(size * 2);
        if ((uint)newSize > BufferSize.Max)
        {
            newSize = BufferSize.Max;
        }
        return newSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (current.IsNull)
        {
            firstBufferWritten += count;
        }
        else
        {
            current.Advance(count);
        }
        totalWritten += count;
    }

    public byte[] ToArrayAndReset()
    {
        if (totalWritten == 0) return Array.Empty<byte>();

        var result = new byte[totalWritten];
        var dest = result.AsSpan();

        if (UseFirstBuffer)
        {
            firstBuffer.AsSpan(0, firstBufferWritten).CopyTo(dest);
            dest = dest.Slice(firstBufferWritten);
        }

        if (buffers.Count > 0)
        {
#if NET7_0_OR_GREATER
            foreach (ref var item in CollectionsMarshal.AsSpan(buffers))
#else
            foreach (var item in buffers)
#endif
            {
                item.WrittenSpan.CopyTo(dest);
                dest = dest.Slice(item.WrittenCount);
                item.Clear(); // reset _buffer-segment in this loop to avoid iterate twice for Reset
            }
        }

        if (!current.IsNull)
        {
            current.WrittenSpan.CopyTo(dest);
            current.Clear();
        }

        ResetCore();
        return result;
    }

    public async ValueTask WriteToAndResetAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (totalWritten == 0) return;

        if (UseFirstBuffer)
        {
            await stream.WriteAsync(firstBuffer.AsMemory(0, firstBufferWritten), cancellationToken).ConfigureAwait(false);
        }

        if (buffers.Count > 0)
        {
            foreach (var item in buffers)
            {
                await stream.WriteAsync(item.WrittenMemory, cancellationToken).ConfigureAwait(false);
                item.Clear(); // reset
            }
        }

        if (!current.IsNull)
        {
            await stream.WriteAsync(current.WrittenMemory, cancellationToken).ConfigureAwait(false);
            current.Clear();
        }

        ResetCore();
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    // reset without list's BufferSegment element
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ResetCore()
    {
        firstBufferWritten = 0;
        buffers.Clear();
        totalWritten = 0;
        current = default;
        nextBufferSize = InitialBufferSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        if (totalWritten == 0) return;
#if NET6_0_OR_GREATER
        foreach (ref var item in CollectionsMarshal.AsSpan(buffers))
#else
        foreach (var item in buffers)
#endif
        {
            item.Clear();
        }
        current.Clear();
        ResetCore();
    }

    public struct Enumerator : IEnumerator<Memory<byte>>
    {
        LinkedBufferWriter parent;
        State state;
        Memory<byte> current;
        List<BufferSegment>.Enumerator buffersEnumerator;

        public Enumerator(LinkedBufferWriter parent)
        {
            this.parent = parent;
            this.state = default;
            this.current = default;
            this.buffersEnumerator = default;
        }

        public Memory<byte> Current => current;

        object IEnumerator.Current => throw new NotSupportedException();

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (state == State.FirstBuffer)
            {
                state = State.BuffersInit;

                if (parent.UseFirstBuffer)
                {
                    current = parent.firstBuffer.AsMemory(0, parent.firstBufferWritten);
                    return true;
                }
            }

            if (state == State.BuffersInit)
            {
                state = State.BuffersIterate;

                buffersEnumerator = parent.buffers.GetEnumerator();
            }

            if (state == State.BuffersIterate)
            {
                if (buffersEnumerator.MoveNext())
                {
                    current = buffersEnumerator.Current.WrittenMemory;
                    return true;
                }

                buffersEnumerator.Dispose();
                state = State.Current;
            }

            if (state == State.Current)
            {
                state = State.End;

                current = parent.current.WrittenMemory;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            throw new NotSupportedException();
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