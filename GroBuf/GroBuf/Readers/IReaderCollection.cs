namespace GroBuf.Readers
{
    internal interface IReaderCollection
    {
        IReaderBuilder<T> GetReaderBuilder<T>();
    }
}