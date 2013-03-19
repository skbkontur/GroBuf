using System;

namespace GroBuf.Writers
{
    internal class PrimitivesWriterBuilder : WriterBuilderBase
    {
        public PrimitivesWriterBuilder(Type type)
            : base(type)
        {
            if(!Type.IsPrimitive && Type != typeof(decimal)) throw new InvalidOperationException("Expected primitive type but was " + Type);
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var typeCode = GroBufTypeCodeMap.GetTypeCode(Type);
            context.WriteTypeCode(typeCode);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            var il = context.Il;
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                context.LoadObj(); // stack: [&result[index], obj]
                il.Stind(typeof(byte)); // result[index] = obj
                context.IncreaseIndexBy1(); // index = index + 1
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                context.LoadObj(); // stack: [&result[index], obj]
                il.Stind(typeof(short)); // result[index] = obj
                context.IncreaseIndexBy2(); // index = index + 2
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                context.LoadObj(); // stack: [&result[index], obj]
                il.Stind(typeof(int)); // result[index] = obj
                context.IncreaseIndexBy4(); // index = index + 4
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                context.LoadObj(); // stack: [&result[index], obj]
                il.Stind(typeof(long)); // result[index] = obj
                context.IncreaseIndexBy8(); // index = index + 8
                break;
            case GroBufTypeCode.Single:
                context.LoadObj(); // stack: [&result[index], obj]
                il.Stind(typeof(float)); // result[index] = obj
                context.IncreaseIndexBy4(); // index = index + 4
                break;
            case GroBufTypeCode.Double:
                context.LoadObj(); // stack: [&result[index], obj]
                il.Stind(typeof(double)); // result[index] = obj
                context.IncreaseIndexBy8(); // index = index + 8
                break;
            case GroBufTypeCode.Decimal:
                context.LoadObjByRef(); // stack: [&result[index], &obj]
                il.Ldind(typeof(long)); // stack: [&result[index], (int64)*obj]
                il.Stind(typeof(long)); // result[index] = (int64)*obj
                context.IncreaseIndexBy8(); // index = index + 8
                context.GoToCurrentLocation(); // stack: [&result[index]]
                context.LoadObjByRef(); // stack: [&result[index], &obj]
                il.Ldc_I4(8); // stack: [&result[index], &obj, 8]
                il.Add(); // stack: [&result[index], &obj + 8]
                il.Ldind(typeof(long)); // stack: [&result[index], *(&obj+8)]
                il.Stind(typeof(long)); // result[index] = (int64)*(obj + 8)
                context.IncreaseIndexBy8(); // index = index + 8
                break;
            default:
                throw new NotSupportedException();
            }
        }
    }
}