using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal class PrimitivesWriterBuilder<T> : WriterBuilderWithoutParams<T>
    {
        public PrimitivesWriterBuilder(IWriterCollection writerCollection)
            : base(writerCollection)
        {
            if(!Type.IsPrimitive) throw new InvalidOperationException("Expected primitive type but was " + Type);
        }

        protected override void WriteNotEmpty(WriterBuilderContext context)
        {
            var typeCode = GroBufHelpers.GetTypeCode(Type);
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
                context.Il.Emit(OpCodes.Ldc_I4_2);
                context.EnsureSize();
                context.WriteTypeCode(typeCode);
                context.GoToCurrentLocation(); // stack: [&result[index]]
                context.LoadObj(); // stack: [&result[index], obj]
                context.Il.Emit(OpCodes.Stind_I1); // result[index] = obj
                context.IncreaseIndexBy1(); // index = index + 1
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                context.Il.Emit(OpCodes.Ldc_I4_3);
                context.EnsureSize();
                context.WriteTypeCode(typeCode);
                context.GoToCurrentLocation(); // stack: [&result[index]]
                context.LoadObj(); // stack: [&result[index], obj]
                context.Il.Emit(OpCodes.Stind_I2); // result[index] = obj
                context.IncreaseIndexBy2(); // index = index + 2
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                context.Il.Emit(OpCodes.Ldc_I4_5);
                context.EnsureSize();
                context.WriteTypeCode(typeCode);
                context.GoToCurrentLocation(); // stack: [&result[index]]
                context.LoadObj(); // stack: [&result[index], obj]
                context.Il.Emit(OpCodes.Stind_I4); // result[index] = obj
                context.IncreaseIndexBy4(); // index = index + 4
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                context.Il.Emit(OpCodes.Ldc_I4, 9);
                context.EnsureSize();
                context.WriteTypeCode(typeCode);
                context.GoToCurrentLocation(); // stack: [&result[index]]
                context.LoadObj(); // stack: [&result[index], obj]
                context.Il.Emit(OpCodes.Stind_I8); // result[index] = obj
                context.IncreaseIndexBy8(); // index = index + 8
                break;
            case GroBufTypeCode.Single:
                context.Il.Emit(OpCodes.Ldc_I4_5);
                context.EnsureSize();
                context.WriteTypeCode(typeCode);
                context.GoToCurrentLocation(); // stack: [&result[index]]
                context.LoadObj(); // stack: [&result[index], obj]
                context.Il.Emit(OpCodes.Stind_R4); // result[index] = obj
                context.IncreaseIndexBy4(); // index = index + 4
                break;
            case GroBufTypeCode.Double:
                context.Il.Emit(OpCodes.Ldc_I4, 9);
                context.EnsureSize();
                context.WriteTypeCode(typeCode);
                context.GoToCurrentLocation(); // stack: [&result[index]]
                context.LoadObj(); // stack: [&result[index], obj]
                context.Il.Emit(OpCodes.Stind_R8); // result[index] = obj
                context.IncreaseIndexBy8(); // index = index + 8
                break;
            default:
                throw new NotSupportedException();
            }
        }
    }
}