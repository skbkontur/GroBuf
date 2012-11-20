using System.Reflection;

namespace GroBuf.Writers
{
    internal interface IWriterBuilder
    {
        MethodInfo BuildWriter(WriterTypeBuilderContext writerTypeBuilderContext);
    }
}