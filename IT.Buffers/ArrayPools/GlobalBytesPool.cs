using System.Buffers;

namespace IT.Buffers;

public class GlobalBytesPool : ArrayPool<byte>
{
    public static readonly GlobalBytesPool Global = new();

    private readonly int _maximumLength;

    public GlobalBytesPool()
    {
        _maximumLength = BufferSize.GB;
    }

    public GlobalBytesPool(int maximumLength)
    {
        _maximumLength = maximumLength;
    }

    public RentedArrayType GetRentedType(int minimumLength)
    {
        return minimumLength == 0 || minimumLength > _maximumLength
            ? RentedArrayType.None : RentedArrayType.Shared;
    }

    public RentedArray<byte> RentArray(int minimumLength)
    {
        var array = Shared.Rent(minimumLength);
        return new(array, 0, minimumLength, GetRentedType(minimumLength));
    }

    public override byte[] Rent(int minimumLength)
    {
        return Shared.Rent(minimumLength);
    }

    public override void Return(byte[] array, bool clearArray = false)
    {
        Shared.Return(array, clearArray);
    }
}