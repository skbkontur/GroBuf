using System;

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
            var il = context.Il;
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                il.Ldc_I4(2);
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                il.Ldc_I4(3);
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Ldc_I4(5);
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Ldc_I4(9);
                break;
            case GroBufTypeCode.Single:
                il.Ldc_I4(5);
                break;
            case GroBufTypeCode.Double:
                il.Ldc_I4(9);
                break;
            case GroBufTypeCode.Decimal:
                il.Ldc_I4(17);
                break;
            default:
                throw new NotSupportedException("Type '" + Type + "' is not supported");
            }
        }
    }
}