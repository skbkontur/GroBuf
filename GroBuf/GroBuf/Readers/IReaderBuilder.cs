using System.Reflection;

namespace GroBuf.Readers
{
    internal interface IReaderBuilder
    {
        MethodInfo BuildReader(ReaderTypeBuilderContext readerTypeBuilderContext);
    }
}