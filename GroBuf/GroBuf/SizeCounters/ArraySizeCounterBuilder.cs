using System;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class ArraySizeCounterBuilder : SizeCounterBuilderBase
    {
        public ArraySizeCounterBuilder(Type type)
            : base(type)
        {
            if(Type != typeof(Array))
            {
                if(!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
                if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
                elementType = Type.GetElementType();
            }
            else elementType = typeof(object);
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var emptyLabel = context.Il.DefineLabel("empty");
            context.LoadObj(); // stack: [obj]
            context.Il.Brfalse(emptyLabel); // if(obj == null) goto empty;
            context.LoadObj(); // stack: [obj]
            context.Il.Ldlen(); // stack: [obj.Length]
            context.Il.Brtrue(notEmptyLabel); // if(obj.Length != 0) goto notEmpty;
            context.Il.MarkLabel(emptyLabel);
            return true;
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(9); // stack: [9 = size] 9 = type code + data length + array length

            var length = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [9, obj]
            il.Ldlen(); // stack: [9, obj.Length]
            il.Stloc(length); // length = obj.Length; stack: [9]
            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [9, 0]
            il.Stloc(i); // i = 0; stack: [9]
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            context.LoadObj(); // stack: [size, obj]
            il.Ldloc(i); // stack: [size, obj, i]
            il.Ldelem(elementType);
            il.Ldc_I4(1); // stack: [size, obj[i], true]
            il.Call(context.Context.GetCounter(elementType)); // stack: [size, writer(obj[i], true) = itemSize]
            il.Add(); // stack: [size + itemSize]
            il.Ldloc(length); // stack: [size, length]
            il.Ldloc(i); // stack: [size, length, i]
            il.Ldc_I4(1); // stack: [size, length, i, 1]
            il.Add(); // stack: [size, length, i + 1]
            il.Dup(); // stack: [size, length, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [size, length, i]
            il.Bgt(typeof(int), cycleStartLabel); // if(length > i) goto cycleStart; stack: [size]
        }

        private readonly Type elementType;
    }
}