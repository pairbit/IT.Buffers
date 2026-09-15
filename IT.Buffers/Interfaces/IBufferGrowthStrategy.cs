namespace IT.Buffers;

public interface IBufferGrowthStrategy
{
    int GetBufferSize<T>();

    int Grow(int size);
}