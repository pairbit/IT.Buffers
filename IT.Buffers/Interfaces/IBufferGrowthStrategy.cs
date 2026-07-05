namespace IT.Buffers.Interfaces;

public interface IBufferGrowthStrategy
{
    int GetBufferSize<T>();

    int GetFirstBufferSize(int size);

    int Grow(int size);
}