using System;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class DateTimeSizeCounterBuilder : SizeCounterBuilderBase<DateTime>
    {
        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            context.Il.Emit(OpCodes.Ldc_I4, 9);
        }
    }
}