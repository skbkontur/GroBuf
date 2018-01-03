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
                context.Il.Call(Type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()); // stack: [obj.Count]
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

            context.WriteTypeCode(GroBufTypeCodeMap.GetTypeCode(elementType.MakeArrayType()));
            var size = il.DeclareLocal(typeof(int));
            il.Ldc_I4(4);
            context.AssertLength();
            context.LoadObj(); // stack: [obj]
            il.Call(Type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()); // stack: [obj.Count]
            CountArraySize(elementType, il); // stack: [obj size]
            il.Stloc(size); // size = obj size; stack: []
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(size); // stack: [&result[index], size]
            il.Stind(typeof(int)); // result[index] = size; stack: []
            context.IncreaseIndexBy4(); // index = index + 4; stack: []

            il.Ldloc(size);
            context.AssertLength();

            context.LoadObj(); // stack: [obj]
            var slotType = Type.GetNestedType("Slot", BindingFlags.NonPublic).MakeGenericType(Type.GetGenericArguments());
            var slots = il.DeclareLocal(slotType.MakeArrayType());
            il.Ldfld(Type.GetField("m_slots", BindingFlags.Instance | BindingFlags.NonPublic));
            il.Stloc(slots);

            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("m_lastIndex", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.m_lastIndex]
            il.Dup();
            var count = context.LocalInt;
            il.Stloc(count); // count = obj.m_lastIndex; stack: [count]
            var writeDataLengthLabel = il.DefineLabel("writeDataLength");
            il.Brfalse(writeDataLengthLabel);

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
            il.Blt(nextLabel, false); // if(slot.hashCode < 0) goto next; stack: [current]
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
            il.Bgt(cycleStartLabel, false); // if(count > i) goto cycleStart; stack: [current]
            il.Pop(); // stack: []

            il.MarkLabel(writeDataLengthLabel);
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(size); // stack: [ref index, index, size]
            il.Add(); // stack: [ref index, index + size]
            il.Stind(typeof(int)); // index = index + size
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