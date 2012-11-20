using System;

namespace GroBuf.SizeCounters
{
    internal interface ISizeCounterCollection
    {
        ISizeCounterBuilder GetSizeCounterBuilder(Type type);
    }
}