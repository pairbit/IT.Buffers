using System;
using System.Buffers;

namespace IT.Buffers;

public sealed class LimitedBytesPool : ArrayPool<byte>
{
    public static readonly LimitedBytesPool LimitedShared = new();

    private readonly int _maximumLength;

    public LimitedBytesPool()
    {
        _maximumLength = BufferSize.GB;
    }

    public LimitedBytesPool(int maximumLength)
    {
        if (maximumLength < BufferSize.Min || maximumLength > BufferSize.GB)
            throw new ArgumentOutOfRangeException(nameof(maximumLength));

        _maximumLength = maximumLength;
    }

    public RentedArrayType GetRentedType(int minimumLength)
    {
        return minimumLength == 0 || minimumLength > _maximumLength
            ? RentedArrayType.None : RentedArrayType.Shared;
    }

    public RentedArray<byte> RentArray(int minimumLength)
    {
        if (minimumLength == 0) return new([]);
        if (minimumLength > _maximumLength)
        {
            return new(
#if NET
                GC.AllocateUninitializedArray<byte>(minimumLength)
#else
                new byte[minimumLength]
#endif
                );
        }

        var array = Rent(minimumLength);
        return new(array, 0, minimumLength, RentedArrayType.Shared);
    }

    public override byte[] Rent(int minimumLength)
    {
        if (minimumLength > _maximumLength)
        {
            return
#if NET
                GC.AllocateUninitializedArray<byte>(minimumLength)
#else
                new byte[minimumLength]
#endif
                ;
        }

        return Shared.Rent(minimumLength);
    }

    public override void Return(byte[] array, bool clearArray = false)
    {
        if (array != null && array.Length > 0 && array.Length <= _maximumLength)
        {
            Shared.Return(array, clearArray);
        }
    }
}