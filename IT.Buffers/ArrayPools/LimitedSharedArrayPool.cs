using IT.Buffers.Internal;
using System;
using System.Buffers;

namespace IT.Buffers;

public sealed class LimitedSharedArrayPool<T> : ArrayPool<T>
{
    private readonly int _maximumLength;

    public LimitedSharedArrayPool(int maximumLength)
    {
        if (maximumLength < BufferSize.Min || maximumLength > BufferSize.GB)
            throw new ArgumentOutOfRangeException(nameof(maximumLength));

        _maximumLength = maximumLength;
    }

    public override T[] Rent(int minimumLength)
    {
        if (minimumLength > _maximumLength)
        {
            return xArray.AllocateUninitialized<T>(minimumLength);
        }

        return Shared.Rent(minimumLength);
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        if (array != null && array.Length > 0)
        {
            if (array.Length <= _maximumLength)
            {
                Shared.Return(array, clearArray);
            }
            else if (clearArray)
            {
                array.Clear();
            }
        }
    }
}