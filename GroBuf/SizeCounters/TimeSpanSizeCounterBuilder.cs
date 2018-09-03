using System;

namespace GroBuf.SizeCounters
{
    internal class TimeSpanSizeCounterBuilder : SizeCounterBuilderBase
    {
        public TimeSpanSizeCounterBuilder()
            : base(typeof(TimeSpan))
        {
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            context.Il.Ldc_I4(9); // stack: [9]
        }

        protected override bool IsReference => false;
    }
}