namespace IT.Buffers.Interfaces;

public interface IBufferGrowthStrategy
{
    int FirstBufferSize { get; }

    int NextBufferSize { get; }

    int Grow(int size);
}