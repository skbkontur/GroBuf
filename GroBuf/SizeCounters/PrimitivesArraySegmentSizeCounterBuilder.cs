using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class PrimitivesArraySegmentSizeCounterBuilder : SizeCounterBuilderBase
    {
        public PrimitivesArraySegmentSizeCounterBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(ArraySegment<>)))
                throw new InvalidOperationException("An array segment expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
            arrayField = Type.GetField("_array", BindingFlags.Instance | BindingFlags.NonPublic);
            countField = Type.GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!elementType.IsPrimitive)
                throw new NotSupportedException("Array segment of primitive type expected but was '" + Type + "'");
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            context.LoadObjByRef(); // stack: [ref obj]
            il.Ldfld(arrayField); // stack: [obj._array]
            if (context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
                il.Brtrue(notEmptyLabel); // if(obj._array != null) goto notEmpty;
            else
            {
                var emptyLabel = il.DefineLabel("empty");
                il.Brfalse(emptyLabel); // if(obj._array == null) goto empty;
                context.LoadObjByRef(); // stack: [ref obj]
                il.Ldfld(countField); // stack: [obj._count]
                il.Brtrue(notEmptyLabel); // if(obj._count != 0) goto notEmpty;
                il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override bool IsReference { get { return false; } }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(5); // stack: [5 = size] 5 = type code + data length
            context.LoadObjByRef(); // stack: [5, ref obj]
            il.Ldfld(countField); // stack: [5, obj._count]
            CountArraySize(elementType, il); // stack: [5, obj length]
            il.Add(); // stack: [5 + obj length]
        }

        private static void CountArraySize(Type elementType, GroboIL il)
        {
            var typeCode = GroBufTypeCodeMap.GetTypeCode(elementType);
            switch (typeCode)
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

        private readonly Type elementType;
        private readonly FieldInfo arrayField;
        private readonly FieldInfo countField;
    }
}