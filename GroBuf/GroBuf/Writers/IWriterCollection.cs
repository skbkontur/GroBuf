namespace SKBKontur.GroBuf.Writers
{
    internal interface IWriterCollection
    {
        WriterDelegate<T> GetWriter<T>();
    }
}