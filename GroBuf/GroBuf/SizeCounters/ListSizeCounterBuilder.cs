using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class ListSizeCounterBuilder : SizeCounterBuilderBase
    {
        public ListSizeCounterBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(List<>)))
                throw new InvalidOperationException("Expected list but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var emptyLabel = context.Il.DefineLabel("empty");
            context.LoadObj(); // stack: [obj]
            context.Il.Brfalse(emptyLabel); // if(obj == null) goto empty;
            context.LoadObj(); // stack: [obj]
            context.Il.Ldfld(Type.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.Count]
            context.Il.Brtrue(notEmptyLabel); // if(obj.Count != 0) goto notEmpty;
            context.Il.MarkLabel(emptyLabel);
            return true;
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(9); // stack: [9 = size] 9 = type code + data length + array length

            var count = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [9, obj]
            il.Ldfld(Type.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [9, obj.Count]
            il.Stloc(count); // count = obj.Count; stack: [9]
            var items = il.DeclareLocal(elementType.MakeArrayType());
            context.LoadObj(); // stack: [9, obj]
            il.Ldfld(Type.GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [9, obj._items]
            il.Stloc(items); // items = obj._items; stack: [9]
            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [9, 0]
            il.Stloc(i); // i = 0; stack: [9]
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            il.Ldloc(items); // stack: [size, items]
            il.Ldloc(i); // stack: [size, items, i]
            il.Ldelem(elementType);
            il.Ldc_I4(1); // stack: [size, obj[i], true]
            context.CallSizeCounter(elementType); // stack: [size, writer(obj[i], true) = itemSize]
            il.Add(); // stack: [size + itemSize]
            il.Ldloc(count); // stack: [size, length]
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