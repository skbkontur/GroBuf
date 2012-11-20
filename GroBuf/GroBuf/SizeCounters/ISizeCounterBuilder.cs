using System.Reflection;

namespace GroBuf.SizeCounters
{
    internal interface ISizeCounterBuilder
    {
        MethodInfo BuildSizeCounter(SizeCounterTypeBuilderContext sizeCounterTypeBuilderContext);
    }
}