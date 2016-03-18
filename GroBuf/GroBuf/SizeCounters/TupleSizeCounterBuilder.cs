using System;

namespace GroBuf.SizeCounters
{
    internal class TupleSizeCounterBuilder : SizeCounterBuilderBase
    {
        public TupleSizeCounterBuilder(Type type)
            : base(type)
        {
            if (!Type.IsTuple())
                throw new InvalidOperationException("A tuple expected but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            foreach(var argumentType in Type.GetGenericArguments())
                context.BuildConstants(argumentType);
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            il.Ldc_I4(5); // stack: [5 = size]

            var genericArguments = Type.GetGenericArguments();
            for(int i = 0; i < genericArguments.Length; ++i)
            {
                var property = Type.GetProperty("Item" + (i + 1));
                var getter = property.GetGetMethod();
                context.LoadObj(); // stack: [size, obj]
                il.Call(getter); // stack: [size, obj.Item{i}]
                il.Ldc_I4(1); // stack: [size, obj.Item{i}, true]
                context.LoadContext(); // stack: [size, obj.Item{i}, true, context]
                context.CallSizeCounter(genericArguments[i]); // stack: [size, writers[i](obj.Item{i}, true, context) = memberSize]
                il.Add(); // stack: [size + memberSize => size]
            }
        }

        protected override bool IsReference { get { return true; } }
    }
}