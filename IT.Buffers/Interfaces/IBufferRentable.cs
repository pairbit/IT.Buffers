namespace IT.Buffers.Interfaces;

public interface IBufferRentable
{
    bool IsRented { get; }

    public void MakeRented();
}