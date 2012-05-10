using System;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class PrimitivesWriterBuilder<T> : WriterBuilderBase<T>
    {
        public PrimitivesWriterBuilder()
        {
            if(!Type.IsPrimitive) throw new InvalidOperationException("Expected primitive type but was " + Type);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var typeCode = GroBufTypeCodeMap.GetTypeCode(Type);
            context.WriteTypeCode(typeCode);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                context.Il.Emit(OpCodes.Stind_I1); // result[index] = obj
                context.IncreaseIndexBy1(); // index = index + 1
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                context.Il.Emit(OpCodes.Stind_I2); // result[index] = obj
                context.IncreaseIndexBy2(); // index = index + 2
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                context.Il.Emit(OpCodes.Stind_I4); // result[index] = obj
                context.IncreaseIndexBy4(); // index = index + 4
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                context.Il.Emit(OpCodes.Stind_I8); // result[index] = obj
                context.IncreaseIndexBy8(); // index = index + 8
                break;
            case GroBufTypeCode.Single:
                context.Il.Emit(OpCodes.Stind_R4); // result[index] = obj
                context.IncreaseIndexBy4(); // index = index + 4
                break;
            case GroBufTypeCode.Double:
                context.Il.Emit(OpCodes.Stind_R8); // result[index] = obj
                context.IncreaseIndexBy8(); // index = index + 8
                break;
            default:
                throw new NotSupportedException();
            }
        }
    }
}