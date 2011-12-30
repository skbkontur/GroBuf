namespace SKBKontur.GroBuf.Readers
{
    internal interface IReaderCollection
    {
        ReaderDelegate<T> GetReader<T>();
    }
}