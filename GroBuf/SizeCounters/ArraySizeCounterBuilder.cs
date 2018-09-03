using System;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class ArraySizeCounterBuilder : SizeCounterBuilderBase
    {
        public ArraySizeCounterBuilder(Type type)
            : base(type)
        {
            if (!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
            if (Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
            elementType = Type.GetElementType();
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            context.LoadObj(); // stack: [obj]
            if (context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
                il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            else
            {
                var emptyLabel = il.DefineLabel("empty");
                il.Brfalse(emptyLabel); // if(obj == null) goto empty;
                context.LoadObj(); // stack: [obj]
                il.Ldlen(); // stack: [obj.Length]
                il.Brtrue(notEmptyLabel); // if(obj.Length != 0) goto notEmpty;
                il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override bool IsReference => true;

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(9); // stack: [9 = size] 9 = type code + data length + array length

            var length = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [9, obj]
            il.Ldlen(); // stack: [9, obj.Length]
            il.Stloc(length); // length = obj.Length; stack: [9]
            var doneLabel = il.DefineLabel("done");
            il.Ldloc(length); // stack: [size, length]
            il.Brfalse(doneLabel); // if(length == 0) goto done; stack: [size]
            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [size, 0]
            il.Stloc(i); // i = 0; stack: [size]
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            context.LoadObj(); // stack: [size, obj]
            il.Ldloc(i); // stack: [size, obj, i]
            il.Ldelem(elementType);
            il.Ldc_I4(1); // stack: [size, obj[i], true]
            context.LoadContext(); // stack: [size, obj[i], true, context]
            context.CallSizeCounter(elementType); // stack: [size, writer(obj[i], true, context) = itemSize]
            il.Add(); // stack: [size + itemSize]
            il.Ldloc(length); // stack: [size, length]
            il.Ldloc(i); // stack: [size, length, i]
            il.Ldc_I4(1); // stack: [size, length, i, 1]
            il.Add(); // stack: [size, length, i + 1]
            il.Dup(); // stack: [size, length, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [size, length, i]
            il.Bgt(cycleStartLabel, false); // if(length > i) goto cycleStart; stack: [size]
            il.MarkLabel(doneLabel);
        }

        private readonly Type elementType;
    }
}