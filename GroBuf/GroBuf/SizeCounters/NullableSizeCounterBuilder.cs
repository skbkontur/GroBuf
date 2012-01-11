using System;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class NullableSizeCounterBuilder<T> : SizeCounterBuilderBase<T>
    {
        public NullableSizeCounterBuilder()
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                throw new InvalidOperationException("Expected nullable but was '" + Type + "'");
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            context.LoadObjByRef(); // stack: [&obj]
            il.Emit(OpCodes.Call, Type.GetProperty("Value").GetGetMethod()); // stack: [obj.Value]
            context.LoadWriteEmpty(); // stack: [obj.Value, writeEmpty]
            il.Emit(OpCodes.Call, context.Context.GetCounter(Type.GetGenericArguments()[0])); // stack: [counter(obj.Value, writeEmpty)]
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, Label notEmptyLabel)
        {
            context.LoadObjByRef(); // stack: [&obj]
            context.Il.Emit(OpCodes.Call, Type.GetProperty("HasValue").GetGetMethod()); // stack: obj.HasValue
            context.Il.Emit(OpCodes.Brtrue, notEmptyLabel); // if(obj.HasValue) goto notEmpty;
            return true;
        }
    }
}