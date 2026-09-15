using System;

namespace IT.Buffers;

//Интерфейс поможет сделать общее хранилище всех пулов.
//для очистки всех пулов и контроля памяти
public interface IBufferPool
{
    int Id { get; }

    Type BufferType { get; }

    //bool IsBounded => BoundedLimit > 0;

    //int BoundedLimit { get; }

    //void Clear();

    //void Trim(int limit);
}