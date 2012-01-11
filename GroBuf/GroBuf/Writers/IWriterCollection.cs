namespace GroBuf.Writers
{
    internal interface IWriterCollection
    {
        IWriterBuilder<T> GetWriterBuilder<T>();
    }
}