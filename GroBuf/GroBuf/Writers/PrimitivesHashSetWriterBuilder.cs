using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class PrimitivesHashSetWriterBuilder : WriterBuilderBase
    {
        public PrimitivesHashSetWriterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(HashSet<>)))
                throw new InvalidOperationException("HashSet expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
            if(!elementType.IsPrimitive)
                throw new NotSupportedException("HashSet of primitive type expected but was '" + Type + "'");
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
                context.Il.Ldfld(Type.GetField("m_count", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.Count]
                context.Il.Brtrue(notEmptyLabel); // if(obj.Count != 0) goto notEmpty;
                context.Il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;

            context.WriteTypeCode(GroBufTypeCodeMap.GetTypeCode(elementType.MakeArrayType()));
            var size = il.DeclareLocal(typeof(int));
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            var count = il.DeclareLocal(typeof(int));
            context.Il.Ldfld(Type.GetField("m_count", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [&result[index], obj.Count]
            il.Stloc(count); // count = obj.Count; stack: [&result[index]]
            il.Ldloc(count); // stack: [&result[index], count]
            CountArraySize(elementType, il); // stack: [&result[index], obj size]
            il.Dup(); // stack: [&result[index], obj size, obj size]
            il.Stloc(size); // size = obj size; stack: [&result[index], obj size]
            il.Stind(typeof(int)); // result[index] = size; stack: []
            context.IncreaseIndexBy4(); // index = index + 4; stack: []

            il.Ldloc(size); // stack: []
            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(size == 0) goto done; stack: []

            context.LoadObj(); // stack: [obj]
            var slotType = Type.GetNestedType("Slot", BindingFlags.NonPublic).MakeGenericType(Type.GetGenericArguments());
            var slots = il.DeclareLocal(slotType.MakeArrayType());
            il.Ldfld(Type.GetField("m_slots", BindingFlags.Instance | BindingFlags.NonPublic));
            il.Stloc(slots);

            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [0]
            il.Stloc(i); // i = 0; stack: []
            context.GoToCurrentLocation(); // stack: [&result[index]]
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            il.Ldloc(slots); // stack: [current, slots]
            il.Ldloc(i); // stack: [current, slots, i]
            il.Ldelema(slotType); // stack: [current, &slots[i]]
            il.Dup(); // stack: [current, &slots[i], &slots[i]]
            var slot = il.DeclareLocal(slotType.MakeByRefType());
            il.Stloc(slot); // slot = &slots[i]; stack: [current, slot]
            il.Ldfld(slotType.GetField("hashCode", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [current, slot.hashCode]
            il.Ldc_I4(0); // stack: [current, slot.hashCode, 0]
            var nextLabel = il.DefineLabel("next");
            il.Blt(typeof(int), nextLabel); // if(slot.hashCode < 0) goto next; stack: [current]
            il.Dup(); // stack: [current, current]
            il.Ldloc(slot); // stack: [current, current, slot]
            il.Ldfld(slotType.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [current, current, slot.value]
            il.Stind(elementType); // *current = slot.value; stack: [current]
            LoadItemSize(elementType, il); // stack: [current, item size]
            il.Add(); // stack: [current + item size]
            il.MarkLabel(nextLabel);
            il.Ldloc(count); // stack: [current, count]
            il.Ldloc(i); // stack: [current, count, i]
            il.Ldc_I4(1); // stack: [current, count, i, 1]
            il.Add(); // stack: [current, count, i + 1]
            il.Dup(); // stack: [current, count, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [current, count, i]
            il.Bgt(typeof(int), cycleStartLabel); // if(count > i) goto cycleStart; stack: [current]

            il.Pop(); // stack: []
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(size); // stack: [ref index, index, size]
            il.Add(); // stack: [ref index, index + size]
            il.Stind(typeof(int)); // index = index + size

            il.MarkLabel(doneLabel);
        }

        private static void CountArraySize(Type elementType, GroboIL il)
        {
            var typeCode = GroBufTypeCodeMap.GetTypeCode(elementType);
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                il.Ldc_I4(1);
                il.Shl();
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Ldc_I4(2);
                il.Shl();
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Ldc_I4(3);
                il.Shl();
                break;
            case GroBufTypeCode.Single:
                il.Ldc_I4(2);
                il.Shl();
                break;
            case GroBufTypeCode.Double:
                il.Ldc_I4(3);
                il.Shl();
                break;
            default:
                throw new NotSupportedException("Type '" + elementType + "' is not supported");
            }
        }

        private static void LoadItemSize(Type elementType, GroboIL il)
        {
            var typeCode = GroBufTypeCodeMap.GetTypeCode(elementType);
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                il.Ldc_I4(1);
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                il.Ldc_I4(2);
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Ldc_I4(4);
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Ldc_I4(8);
                break;
            case GroBufTypeCode.Single:
                il.Ldc_I4(4);
                break;
            case GroBufTypeCode.Double:
                il.Ldc_I4(8);
                break;
            default:
                throw new NotSupportedException("Type '" + elementType + "' is not supported");
            }
        }

        private readonly Type elementType;
    }
}