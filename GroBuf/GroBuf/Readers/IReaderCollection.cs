namespace SKBKontur.GroBuf.Readers
{
    internal interface IReaderCollection
    {
        IReaderBuilder<T> GetReader<T>();
    }
}