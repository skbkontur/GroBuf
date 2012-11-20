using System;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class PrimitivesSizeCounterBuilder : SizeCounterBuilderBase
    {
        public PrimitivesSizeCounterBuilder(Type type)
            : base(type)
        {
            if(!Type.IsPrimitive && Type != typeof(decimal)) throw new InvalidOperationException("Expected primitive type but was '" + Type + "'");
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var typeCode = GroBufTypeCodeMap.GetTypeCode(Type);
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                context.Il.Emit(OpCodes.Ldc_I4_2);
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                context.Il.Emit(OpCodes.Ldc_I4_3);
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                context.Il.Emit(OpCodes.Ldc_I4_5);
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                context.Il.Emit(OpCodes.Ldc_I4, 9);
                break;
            case GroBufTypeCode.Single:
                context.Il.Emit(OpCodes.Ldc_I4_5);
                break;
            case GroBufTypeCode.Double:
                context.Il.Emit(OpCodes.Ldc_I4, 9);
                break;
            case GroBufTypeCode.Decimal:
                context.Il.Emit(OpCodes.Ldc_I4, 17);
                break;
            default:
                throw new NotSupportedException("Type '" + Type + "' is not supported");
            }
        }
    }
}