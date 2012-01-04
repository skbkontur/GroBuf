using System.Reflection;

namespace SKBKontur.GroBuf.Writers
{
    internal interface IWriterBuilder<T>
    {
        MethodInfo BuildWriter(WriterTypeBuilderContext writerTypeBuilderContext);
    }
}