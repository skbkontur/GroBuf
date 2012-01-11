using System.Reflection;

namespace GroBuf.Readers
{
    internal interface IReaderBuilder<out T>
    {
        MethodInfo BuildReader(ReaderTypeBuilderContext readerTypeBuilderContext);
    }
}