using System.Reflection;

namespace GroBuf.Counterz
{
    internal interface ISizeCounterBuilder<T>
    {
        MethodInfo BuildCounter(SizeCounterTypeBuilderContext sizeCounterTypeBuilderContext);
    }
}