using System.Reflection;

namespace SKBKontur.GroBuf.Readers
{
    internal interface IReaderBuilder<out T>
    {
        MethodInfo BuildReader(ReaderTypeBuilderContext readerTypeBuilderContext);
    }
}