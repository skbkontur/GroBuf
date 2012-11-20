using System;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class PrimitivesArraySizeCounterBuilder : SizeCounterBuilderBase
    {
        public PrimitivesArraySizeCounterBuilder(Type type)
            : base(type)
        {
            if(Type != typeof(Array))
            {
                if(!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
                if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
                elementType = Type.GetElementType();
                if(!elementType.IsPrimitive) throw new NotSupportedException("Array of primitive type expected but was '" + Type + "'");
            }
            else elementType = typeof(object);
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, Label notEmptyLabel)
        {
            var emptyLabel = context.Il.DefineLabel();
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Brfalse, emptyLabel); // if(obj == null) goto empty;
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Ldlen); // stack: [obj.Length]
            context.Il.Emit(OpCodes.Brtrue, notEmptyLabel); // if(obj.Length != 0) goto notEmpty;
            context.Il.MarkLabel(emptyLabel);
            return true;
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Emit(OpCodes.Ldc_I4, 5); // stack: [5 = size] 5 = type code + data length
            context.LoadObj(); // stack: [5, obj]
            il.Emit(OpCodes.Ldlen); // stack: [5, obj.Length]
            CountArraySize(elementType, il); // stack: [5, obj length]
            il.Emit(OpCodes.Add); // stack: [5 + obj length]
        }

        private static void CountArraySize(Type elementType, ILGenerator il)
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
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Shl);
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Emit(OpCodes.Ldc_I4_2);
                il.Emit(OpCodes.Shl);
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Emit(OpCodes.Ldc_I4_3);
                il.Emit(OpCodes.Shl);
                break;
            case GroBufTypeCode.Single:
                il.Emit(OpCodes.Ldc_I4_2);
                il.Emit(OpCodes.Shl);
                break;
            case GroBufTypeCode.Double:
                il.Emit(OpCodes.Ldc_I4_3);
                il.Emit(OpCodes.Shl);
                break;
            default:
                throw new NotSupportedException("Type '" + elementType + "' is not supported");
            }
        }

        private readonly Type elementType;
    }
}