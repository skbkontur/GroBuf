using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class HashSetSizeCounterBuilder : SizeCounterBuilderBase
    {
        public HashSetSizeCounterBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(HashSet<>)))
                throw new InvalidOperationException("HashSet expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            context.LoadObj(); // stack: [obj]
            if (context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
                context.Il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            else
            {
                var emptyLabel = context.Il.DefineLabel("empty");
                context.Il.Brfalse(emptyLabel); // if(obj == null) goto empty;
                context.LoadObj(); // stack: [obj]
                context.Il.Call(Type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()); // stack: [obj.Count]
                context.Il.Brtrue(notEmptyLabel); // if(obj.Count != 0) goto notEmpty;
                context.Il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override bool IsReference { get { return true; } }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(9); // stack: [9 = size] 9 = type code + data length + hashset count

            context.LoadObj(); // stack: [size, obj]
            var count = il.DeclareLocal(typeof(int));
            il.Ldfld(Type.GetField(PlatformHelpers.HashSetLastIndexFieldName, BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [size, obj.m_lastIndex]
            il.Dup();
            il.Stloc(count); // count = obj.m_lastIndex; stack: [size, count]
            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(!count) goto done; stack: [size]

            context.LoadObj(); // stack: [size, obj]
            var slotType = Type.GetNestedType("Slot", BindingFlags.NonPublic).MakeGenericType(Type.GetGenericArguments());
            var slots = il.DeclareLocal(slotType.MakeArrayType());
            il.Ldfld(Type.GetField(PlatformHelpers.HashSetSlotsFieldName, BindingFlags.Instance | BindingFlags.NonPublic));
            il.Stloc(slots);

            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [9, 0]
            il.Stloc(i); // i = 0; stack: [9]
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            il.Ldloc(slots); // stack: [size, slots]
            il.Ldloc(i); // stack: [size, slots, i]
            il.Ldelema(slotType); // stack: [size, &slots[i]]
            il.Dup(); // stack: [size, &slots[i], &slots[i]]
            var slot = il.DeclareLocal(slotType.MakeByRefType());
            il.Stloc(slot); // slot = &slots[i]; stack: [size, slot]
            il.Ldfld(slotType.GetField("hashCode", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [size, slot.hashCode]
            il.Ldc_I4(0); // stack: [size, slot.hashCode, 0]
            var nextLabel = il.DefineLabel("next");
            il.Blt(nextLabel, false); // if(slot.hashCode < 0) goto next; stack: [size]

            il.Ldloc(slot); // stack: [size, slot]
            il.Ldfld(slotType.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [size, slot.value]
            il.Ldc_I4(1); // stack: [size, slot.value, true]
            context.LoadContext(); // stack: [size, slot.value, true, context]
            context.CallSizeCounter(elementType); // stack: [size, writer(slot.value, true, context) = valueSize]
            il.Add(); // stack: [size + valueSize]

            il.MarkLabel(nextLabel);

            il.Ldloc(count); // stack: [size, count]
            il.Ldloc(i); // stack: [size, count, i]
            il.Ldc_I4(1); // stack: [size, count, i, 1]
            il.Add(); // stack: [size, count, i + 1]
            il.Dup(); // stack: [size, count, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [size, count, i]
            il.Bgt(cycleStartLabel, false); // if(count > i) goto cycleStart; stack: [size]

            il.MarkLabel(doneLabel);
        }

        private readonly Type elementType;
    }
}