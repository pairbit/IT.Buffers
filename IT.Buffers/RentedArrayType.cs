namespace IT.Buffers;

public enum RentedArrayType : byte
{
    /// <summary>
    /// Not rented
    /// </summary>
    None = 0,

    /// <summary>
    /// Rented from an ArrayPool.Shared
    /// </summary>
    Shared = 1,

    /// <summary>
    /// Rented from an GlobalArrayPool.Global
    /// </summary>
    Global = 2,

    /// <summary>
    /// Rented from an external pool
    /// </summary>
    External = 3
}