namespace IT.Buffers.Interfaces;

public interface IBufferGrowthStrategy
{
    int Grow(int size);
}