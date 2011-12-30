namespace SKBKontur.GroBuf.Writers
{
    internal interface IWriterBuilder<in T>
    {
        WriterDelegate<T> BuildWriter();
    }
}