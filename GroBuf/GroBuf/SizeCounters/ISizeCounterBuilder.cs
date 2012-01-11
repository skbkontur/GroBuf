using System.Reflection;

namespace GroBuf.SizeCounters
{
    internal interface ISizeCounterBuilder<T>
    {
        MethodInfo BuildCounter(SizeCounterTypeBuilderContext sizeCounterTypeBuilderContext);
    }
}