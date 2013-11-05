using System;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class NullableSizeCounterBuilder : SizeCounterBuilderBase
    {
        public NullableSizeCounterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                throw new InvalidOperationException("Expected nullable but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(Type.GetGenericArguments()[0]);
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            context.LoadSizeCounter(Type.GetGenericArguments()[0]);

            context.LoadObjByRef(); // stack: [&obj]
            il.Call(Type.GetProperty("Value").GetGetMethod()); // stack: [obj.Value]
            context.LoadWriteEmpty(); // stack: [obj.Value, writeEmpty]
            context.CallSizeCounter(Type.GetGenericArguments()[0]); // stack: [counter(obj.Value, writeEmpty)]
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            context.LoadObjByRef(); // stack: [&obj]
            context.Il.Call(Type.GetProperty("HasValue").GetGetMethod()); // stack: obj.HasValue
            context.Il.Brtrue(notEmptyLabel); // if(obj.HasValue) goto notEmpty;
            return true;
        }
    }
}