using System.Runtime.CompilerServices;

namespace IT.Buffers.Internal;

internal static class SequenceFlags
{
    public const int FlagBitMask = 1 << 31;
    public const int IndexBitMask = ~FlagBitMask;

    public const int ArrayEndMask = FlagBitMask;

    public const int MemoryManagerStartMask = FlagBitMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ArrayToSequenceEnd(int endIndex) => endIndex | ArrayEndMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MemoryManagerToSequenceStart(int startIndex) => startIndex | MemoryManagerStartMask;
}