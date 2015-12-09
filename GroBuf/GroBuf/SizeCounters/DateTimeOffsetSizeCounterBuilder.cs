using System;

namespace GroBuf.SizeCounters
{
    internal class DateTimeOffsetSizeCounterBuilder : SizeCounterBuilderBase
    {
        public DateTimeOffsetSizeCounterBuilder()
            : base(typeof(DateTimeOffset))
        {
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            // TODO call sub size counters
            context.Il.Ldc_I4(1 + 9 + 3); // stack: [13]
        }

        protected override bool IsReference { get { return false; } }
    }
}