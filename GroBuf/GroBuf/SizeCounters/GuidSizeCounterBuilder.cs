using System;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class GuidSizeCounterBuilder : SizeCounterBuilderBase<Guid>
    {
        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            context.Il.Emit(OpCodes.Ldc_I4, 17); // stack: [17]
        }
    }
}