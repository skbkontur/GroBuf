using System.Reflection;

namespace GroBuf.Writers
{
    internal interface IWriterBuilder<T>
    {
        MethodInfo BuildWriter(WriterTypeBuilderContext writerTypeBuilderContext);
    }
}