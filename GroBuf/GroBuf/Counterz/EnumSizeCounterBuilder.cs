using System;
using System.Reflection.Emit;

namespace GroBuf.Counterz
{
    internal class EnumSizeCounterBuilder<T> : SizeCounterBuilderBase<T>
    {
        public EnumSizeCounterBuilder()
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was " + Type);
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            context.Il.Emit(OpCodes.Ldc_I4, 9);
        }
    }
}