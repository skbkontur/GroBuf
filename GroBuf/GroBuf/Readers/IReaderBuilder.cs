namespace SKBKontur.GroBuf.Readers
{
    internal interface IReaderBuilder<out T>
    {
        ReaderDelegate<T> BuildReader();
    }
}