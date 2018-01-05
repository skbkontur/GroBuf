namespace GroBuf.Readers
{
    internal interface IReaderBuilder
    {
        void BuildReader(ReaderTypeBuilderContext readerTypeBuilderContext);
        void BuildConstants(ReaderConstantsBuilderContext context);
    }
}