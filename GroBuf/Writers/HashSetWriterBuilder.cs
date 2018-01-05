using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class HashSetWriterBuilder : WriterBuilderBase
    {
        public HashSetWriterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(HashSet<>)))
                throw new InvalidOperationException("HashSet expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            context.LoadObj(); // stack: [obj]
            if(context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
                context.Il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            else
            {
                var emptyLabel = context.Il.DefineLabel("empty");
                context.Il.Brfalse(emptyLabel); // if(obj == null) goto empty;
                context.LoadObj(); // stack: [obj]
                context.Il.Call(Type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()); // stack: [&result[index], obj.Count]
                context.Il.Brtrue(notEmptyLabel); // if(obj.Count != 0) goto notEmpty;
                context.Il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override bool IsReference { get { return true; } }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.Array);
            context.LoadIndex(); // stack: [index]
            var start = context.LocalInt;
            il.Stloc(start); // start = index
            il.Ldc_I4(8);
            context.AssertLength(); // data length + hashset size = 8
            context.IncreaseIndexBy4(); // index = index + 4
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Call(Type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()); // stack: [&result[index], obj.Count]
            il.Stind(typeof(int)); // *(int*)&result[index] = obj.Count; stack: []
            context.IncreaseIndexBy4(); // index = index + 4; stack: []

            context.LoadObj(); // stack: [obj]
            var count = il.DeclareLocal(typeof(int));
            il.Ldfld(Type.GetField(PlatformHelpers.HashSetLastIndexFieldName, BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.m_lastIndex]
            il.Dup();
            il.Stloc(count); // count = obj.m_lastIndex; stack: []
            var writeDataLengthLabel = il.DefineLabel("writeDataLength");
            il.Brfalse(writeDataLengthLabel);

            context.LoadObj(); // stack: [obj]
            var slotType = Type.GetNestedType("Slot", BindingFlags.NonPublic).MakeGenericType(Type.GetGenericArguments());
            var slots = il.DeclareLocal(slotType.MakeArrayType());
            il.Ldfld(Type.GetField(PlatformHelpers.HashSetSlotsFieldName, BindingFlags.Instance | BindingFlags.NonPublic));
            il.Stloc(slots);

            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [0]
            il.Stloc(i); // i = 0; stack: []
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            il.Ldloc(slots); // stack: [slots]
            il.Ldloc(i); // stack: [slots, i]
            il.Ldelema(slotType); // stack: [&slots[i]]
            il.Dup(); // stack: [&slots[i], &slots[i]]
            var slot = il.DeclareLocal(slotType.MakeByRefType());
            il.Stloc(slot); // slot = &slots[i]; stack: [slot]
            il.Ldfld(slotType.GetField("hashCode", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [slot.hashCode]

            il.Ldc_I4(0); // stack: [slot.hashCode, 0]
            var nextLabel = il.DefineLabel("next");
            il.Blt(nextLabel, false); // if(slot.hashCode < 0) goto next; stack: []

            il.Ldloc(slot); // stack: [slot]
            il.Ldfld(slotType.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [slot.value]
            il.Ldc_I4(1); // stack: [obj[i], true]
            context.LoadResult(); // stack: [obj[i], true, result]
            context.LoadIndexByRef(); // stack: [obj[i], true, result, ref index]
            context.LoadContext(); // stack: [obj[i], true, result, ref index, context]
            context.CallWriter(elementType); // write<elementType>(obj[i], true, result, ref index, context)

            il.MarkLabel(nextLabel);
            il.Ldloc(count); // stack: [count]
            il.Ldloc(i); // stack: [count, i]
            il.Ldc_I4(1); // stack: [count, i, 1]
            il.Add(); // stack: [count, i + 1]
            il.Dup(); // stack: [count, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [count, i]
            il.Bgt(cycleStartLabel, false); // if(count > i) goto cycleStart; stack: []

            il.MarkLabel(writeDataLengthLabel);
            context.LoadResult(); // stack: [result]
            il.Ldloc(start); // stack: [result, start]
            il.Add(); // stack: [result + start]
            context.LoadIndex(); // stack: [result + start, index]
            il.Ldloc(start); // stack: [result + start, index, start]
            il.Sub(); // stack: [result + start, index - start]
            il.Ldc_I4(4); // stack: [result + start, index - start, 4]
            il.Sub(); // stack: [result + start, index - start - 4]
            il.Stind(typeof(int)); // *(int*)(result + start) = index - start - 4
        }

        private readonly Type elementType;
    }
}