namespace GroBuf.SizeCounters
{
    internal interface ISizeCounterBuilder
    {
        void BuildSizeCounter(SizeCounterBuilderContext sizeCounterBuilderContext);
        void BuildConstants(SizeCounterConstantsBuilderContext context);
    }
}