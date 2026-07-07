namespace IT.Buffers.Interfaces;

public interface IBufferGrowthStrategy
{
    int GetBufferSize<T>();

    int Grow(int size);
}