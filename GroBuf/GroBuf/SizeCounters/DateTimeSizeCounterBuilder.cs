using System;

namespace GroBuf.SizeCounters
{
    internal class DateTimeSizeCounterBuilder : SizeCounterBuilderBase
    {
        public DateTimeSizeCounterBuilder()
            : base(typeof(DateTime))
        {
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(9); // stack: [9]
        }

        protected override bool IsReference { get { return false; } }
    }
}