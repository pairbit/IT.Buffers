using System;
using System.Buffers;
using System.Diagnostics;

namespace IT.Buffers;

public static class BufferSize
{
    public const int Min = 256;//2^8
    public const int KB = 1024;//2^10
    public const int KB_2 = 2048;//2^11
    public const int KB_4 = 4096;//2^12
    public const int KB_8 = 8192;//2^13
    public const int KB_16 = 16384;//2^14
    public const int KB_32 = 32768;//2^15
    public const int KB_64 = 65536;//2^16
    public const int KB_80 = 81920;
    public const int KB_83 = 84992;
    public const int LOH = 85000;
    public const int KB_128 = 131072;//2^17
    public const int KB_256 = 262144;//2^18
    public const int KB_512 = 524288;//2^19
    public const int MB = 1048576;//2^20
    public const int Max = 0X7FFFFFC7;//2147483591 = 2^31 - 57
    public const int MaxHalf = Max / 2;//1073741795

#if NET
    static BufferSize()
    {
        Debug.Assert(Max == Array.MaxLength);
    }
#endif

    public static int GetDoubleCapacity(int size)
    {
        var newSize = unchecked(size * 2);
        if ((uint)newSize > Max)
        {
            newSize = Max;
        }
        return newSize;
    }

    internal static void CheckAndResizeBuffer<T>(ref T[] buffer, int written, int sizeHint)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        int capacity = buffer.Length;

        Debug.Assert(capacity >= written);

        int freeCapacity = capacity - written;

        // If we've reached ~1GB written, grow to the maximum buffer
        // length to avoid incessant minimal growths causing perf issues.
        if (written >= MaxHalf)
        {
            sizeHint = Math.Max(sizeHint, Max - capacity);
        }

        if (sizeHint > freeCapacity)
        {
            int growBy = Math.Max(sizeHint, capacity);

            int newSize = capacity + growBy;

            if ((uint)newSize > Max)
            {
                newSize = capacity + sizeHint;
                if ((uint)newSize > Max)
                {
                    throw new OutOfMemoryException($"Size {(uint)newSize} > {Max}");
                }
            }

            if (capacity == 0)
            {
                Debug.Assert(written == 0);

                buffer = ArrayPool<T>.Shared.Rent(newSize);

                Debug.Assert(buffer.Length >= written);
            }
            else
            {
                T[] oldBuffer = buffer;

                buffer = ArrayPool<T>.Shared.Rent(newSize);

                Debug.Assert(buffer.Length >= written);

                if (written > 0)
                    oldBuffer.AsSpan(0, written).CopyTo(buffer);

                BufferPool.Return(oldBuffer);
            }

            Debug.Assert(buffer.Length > capacity);
            Debug.Assert(buffer.Length - written > 0);
        }
        
        Debug.Assert(buffer.Length - written >= sizeHint);
    }

    internal static T[] RentBuffer<T>(int sizeHint)
    {
        if (sizeHint <= 0) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        if (sizeHint > Max) throw new OutOfMemoryException($"Size {sizeHint} > {Max}");

        var buffer = ArrayPool<T>.Shared.Rent(sizeHint);

        Debug.Assert(buffer.Length > 0);
        Debug.Assert(buffer.Length >= sizeHint);

        return buffer;
    }
}