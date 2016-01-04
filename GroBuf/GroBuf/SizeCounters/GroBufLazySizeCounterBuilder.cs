using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class GroBufLazySizeCounterBuilder : SizeCounterBuilderBase
    {
        public GroBufLazySizeCounterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(GroBufLazy<>)))
                throw new InvalidOperationException("Expected GroBufLazy but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(Type.GetGenericArguments()[0]);
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("raw", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.raw]
            var countRawLabel = il.DefineLabel("countRaw");
            il.Brtrue(countRawLabel);
            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.value]
            context.LoadWriteEmpty(); // stack: [obj.value, writeEmpty]
            context.LoadContext(); // stack: [obj.value, writeEmpty, context]
            context.CallSizeCounter(Type.GetGenericArguments()[0]); // stack: [counter(obj.value, writeEmpty, context)]
            il.Ret();
            il.MarkLabel(countRawLabel);
            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("data", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.data]
            il.Ldlen(); // stack: [obj.data.Length]
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            var emptyLabel = il.DefineLabel("empty");
            context.LoadObj(); // stack: [obj]
            il.Brfalse(emptyLabel); // if(obj == null) goto empty; stack: []
            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("raw", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.raw]
            il.Brtrue(notEmptyLabel); // if(obj.raw) goto notEmpty; stack: []
            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.value]
            il.Brtrue(notEmptyLabel); // if(obj.value != null) goto notEmpty; stack: []
            il.MarkLabel(emptyLabel);
            return true;
        }

        protected override bool IsReference { get { return false; } }
    }
}