namespace GroBuf.Writers
{
    internal interface IWriterBuilder
    {
        void BuildWriter(WriterTypeBuilderContext writerTypeBuilderContext);
        void BuildConstants(WriterConstantsBuilderContext context);
    }
}