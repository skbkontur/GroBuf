using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class PrimitivesReaderBuilder<T> : ReaderBuilderWithOneParam<T, Delegate[]>
    {
        public PrimitivesReaderBuilder(IReaderCollection readerCollection)
            : base(readerCollection)
        {
            if(!Type.IsPrimitive) throw new InvalidOperationException("Expected primitive type but was " + Type);
        }

        protected override Delegate[] ReadNotEmpty(ReaderBuilderContext<T> context)
        {
            context.IncreaseIndexBy1();
            var readers = BuildPrimitiveValueReaders();
            var il = context.Il;

            context.LoadAdditionalParam(0); // stack: [readers]
            il.Emit(OpCodes.Ldloc, context.TypeCode); // stack: [readers, typeCode]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [readers[typeCode]]
            context.GoToCurrentLocation(); // stack: [readers[typeCode], &data[index]]
            context.SkipValue();
            il.Emit(OpCodes.Call, typeof(PrimitiveValueReaderDelegate).GetMethod("Invoke")); // readers[typeCode](&data[index]); stack: [result]
            return readers;
        }

        private unsafe delegate T PrimitiveValueReaderDelegate(byte* buf);

        private unsafe Delegate[] BuildPrimitiveValueReaders()
        {
            var result = new Delegate[256];
            var defaultReader = BuildDefaultValueReader();
            for(int i = 0; i < 256; ++i)
                result[i] = defaultReader;
            foreach(var typeCode in new[]
                {
                    GroBufTypeCode.Int8, GroBufTypeCode.UInt8,
                    GroBufTypeCode.Int16, GroBufTypeCode.UInt16,
                    GroBufTypeCode.Int32, GroBufTypeCode.UInt32,
                    GroBufTypeCode.Int64, GroBufTypeCode.UInt64,
                    GroBufTypeCode.Single, GroBufTypeCode.Double
                })
                result[(int)typeCode] = BuildPrimitiveValueReader(typeCode);
            return result;
        }

        private PrimitiveValueReaderDelegate BuildDefaultValueReader()
        {
            var type = typeof(T);
            if(!type.IsPrimitive) throw new InvalidOperationException("Attempt to build primitive value reader for a non-primitive type " + type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), type, new[] {typeof(byte).MakePointerType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var result = il.DeclareLocal(type);
            il.Emit(OpCodes.Ldloca, result);
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);
            return (PrimitiveValueReaderDelegate)dynamicMethod.CreateDelegate(typeof(PrimitiveValueReaderDelegate));
        }

        private PrimitiveValueReaderDelegate BuildPrimitiveValueReader(GroBufTypeCode typeCode)
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), Type, new[] {typeof(byte).MakePointerType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var expectedTypeCode = GroBufHelpers.GetTypeCode(Type);
            il.Emit(OpCodes.Ldarg_0); // stack: [address]
            EmitReadPrimitiveValue(il, typeCode); // stack: [value]
            if(Type == typeof(bool))
            {
                il.Emit(OpCodes.Ldc_I4_0); // stack: [0, value]
                il.Emit(OpCodes.Ceq); // stack: [value == 0]
                il.Emit(OpCodes.Ldc_I4_1); // stack: [value == 0, 1]
                il.Emit(OpCodes.Xor); // stack: [value != 0]
            }
            else
                EmitConvertValue(il, typeCode, expectedTypeCode);
            il.Emit(OpCodes.Ret);
            return (PrimitiveValueReaderDelegate)dynamicMethod.CreateDelegate(typeof(PrimitiveValueReaderDelegate));
        }

        private static void EmitConvertValue(ILGenerator il, GroBufTypeCode typeCode, GroBufTypeCode expectedTypeCode)
        {
            if(expectedTypeCode == typeCode)
                return;
            switch(expectedTypeCode)
            {
            case GroBufTypeCode.Int8:
                il.Emit(OpCodes.Conv_I1);
                break;
            case GroBufTypeCode.UInt8:
                il.Emit(OpCodes.Conv_U1);
                break;
            case GroBufTypeCode.Int16:
                il.Emit(OpCodes.Conv_I2);
                break;
            case GroBufTypeCode.UInt16:
                il.Emit(OpCodes.Conv_U2);
                break;
            case GroBufTypeCode.Int32:
                if(typeCode == GroBufTypeCode.Int64 || typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.Double || typeCode == GroBufTypeCode.Single)
                    il.Emit(OpCodes.Conv_I4);
                break;
            case GroBufTypeCode.UInt32:
                if(typeCode == GroBufTypeCode.Int64 || typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.Double || typeCode == GroBufTypeCode.Single)
                    il.Emit(OpCodes.Conv_U4);
                break;
            case GroBufTypeCode.Int64:
                if(typeCode != GroBufTypeCode.UInt64)
                {
                    if(typeCode == GroBufTypeCode.UInt8 || typeCode == GroBufTypeCode.UInt16 || typeCode == GroBufTypeCode.UInt32)
                        il.Emit(OpCodes.Conv_U8);
                    else
                        il.Emit(OpCodes.Conv_I8);
                }
                break;
            case GroBufTypeCode.UInt64:
                if(typeCode != GroBufTypeCode.Int64)
                {
                    if(typeCode == GroBufTypeCode.Int8 || typeCode == GroBufTypeCode.Int16 || typeCode == GroBufTypeCode.Int32)
                        il.Emit(OpCodes.Conv_I8);
                    else
                        il.Emit(OpCodes.Conv_U8);
                }
                break;
            case GroBufTypeCode.Single:
                if(typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.UInt32)
                    il.Emit(OpCodes.Conv_R_Un);
                il.Emit(OpCodes.Conv_R4);
                break;
            case GroBufTypeCode.Double:
                if(typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.UInt32)
                    il.Emit(OpCodes.Conv_R_Un);
                il.Emit(OpCodes.Conv_R8);
                break;
            default:
                throw new NotSupportedException();
            }
        }

        private static void EmitReadPrimitiveValue(ILGenerator il, GroBufTypeCode typeCode)
        {
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
                il.Emit(OpCodes.Ldind_I1);
                break;
            case GroBufTypeCode.UInt8:
                il.Emit(OpCodes.Ldind_U1);
                break;
            case GroBufTypeCode.Int16:
                il.Emit(OpCodes.Ldind_I2);
                break;
            case GroBufTypeCode.UInt16:
                il.Emit(OpCodes.Ldind_U2);
                break;
            case GroBufTypeCode.Int32:
                il.Emit(OpCodes.Ldind_I4);
                break;
            case GroBufTypeCode.UInt32:
                il.Emit(OpCodes.Ldind_U4);
                break;
            case GroBufTypeCode.Int64:
                il.Emit(OpCodes.Ldind_I8);
                break;
            case GroBufTypeCode.UInt64:
                il.Emit(OpCodes.Ldind_I8);
                break;
            case GroBufTypeCode.Single:
                il.Emit(OpCodes.Ldind_R4);
                break;
            case GroBufTypeCode.Double:
                il.Emit(OpCodes.Ldind_R8);
                break;
            default:
                throw new NotSupportedException();
            }
        }
    }
}