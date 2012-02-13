using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class PrimitivesReaderBuilder<T> : ReaderBuilderBase<T>
    {
        public PrimitivesReaderBuilder()
        {
            if(!Type.IsPrimitive) throw new InvalidOperationException("Expected primitive type but was '" + Type + "'");
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext<T> context)
        {
            context.IncreaseIndexBy1();
            var readers = BuildPrimitiveValueReaders(context.Context);
            var readersField = context.Context.BuildConstField<IntPtr[]>("readers_" + Type.Name + "_" + Guid.NewGuid(), field => BuildReadersFieldInitializer(context.Context, field, readers));
            var il = context.Il;

            context.GoToCurrentLocation(); // stack: [&data[index]]
            context.LoadResultByRef(); // stack: [&data[index], ref result]
            context.SkipValue();
            context.LoadField(readersField); // stack: [&data[index], ref result, readers]
            il.Emit(OpCodes.Ldloc, context.TypeCode); // stack: [&data[index], ref result, readers, typeCode]
            il.Emit(OpCodes.Ldelem_I); // stack: [&data[index], ref result, readers[typeCode]]
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), new[] {typeof(byte*), Type.MakeByRefType()}, null); // readers[typeCode](&data[index], ref result); stack: []
        }

        private static Action BuildReadersFieldInitializer(ReaderTypeBuilderContext context, FieldInfo field, MethodInfo[] readers)
        {
            var typeBuilder = context.TypeBuilder;
            var method = typeBuilder.DefineMethod(field.Name + "_Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldc_I4, readers.Length); // stack: [null, readers.Length]
            il.Emit(OpCodes.Newarr, typeof(IntPtr)); // stack: [null, new IntPtr[readers.Length]]
            il.Emit(OpCodes.Stfld, field); // readersField = new IntPtr[readers.Length]
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldfld, field); // stack: [readersField]
            for(int i = 0; i < readers.Length; ++i)
            {
                if(readers[i] == null) continue;
                il.Emit(OpCodes.Dup); // stack: [readersField, readersField]
                il.Emit(OpCodes.Ldc_I4, i); // stack: [readersField, readersField, i]
                il.Emit(OpCodes.Ldftn, readers[i]); // stack: [readersField, readersField, i, readers[i]]
                il.Emit(OpCodes.Stelem_I); // readersField[i] = readers[i]; stack: [readersField]
            }
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            return () => typeBuilder.GetMethod(method.Name).Invoke(null, null);
        }

        private MethodInfo[] BuildPrimitiveValueReaders(ReaderTypeBuilderContext context)
        {
            var result = new MethodInfo[256];
            var defaultReader = BuildDefaultValueReader(context);
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
                result[(int)typeCode] = BuildPrimitiveValueReader(context, typeCode);
            return result;
        }

        private MethodInfo BuildDefaultValueReader(ReaderTypeBuilderContext context)
        {
            var method = context.TypeBuilder.DefineMethod("Default_" + Type.Name + "_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static,
                                                          typeof(void), new[] {typeof(byte*), Type.MakeByRefType()});
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1); // stack: [ref result]
            il.Emit(OpCodes.Initobj, Type); // [result = default(T)]
            il.Emit(OpCodes.Ret);
            return method;
        }

        private MethodInfo BuildPrimitiveValueReader(ReaderTypeBuilderContext context, GroBufTypeCode typeCode)
        {
            var method = context.TypeBuilder.DefineMethod("Read_" + Type.Name + "_from_" + typeCode + "_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static,
                                                          typeof(void), new[] {typeof(byte*), Type.MakeByRefType()});
            var il = method.GetILGenerator();
            var expectedTypeCode = GroBufHelpers.GetTypeCode(Type);
            il.Emit(OpCodes.Ldarg_1); // stack: [ref result]
            il.Emit(OpCodes.Ldarg_0); // stack: [ref result, address]
            EmitReadPrimitiveValue(il, typeCode); // stack: [ref result, value]
            if(Type == typeof(bool))
            {
                il.Emit(OpCodes.Ldc_I4_0); // stack: [ref result, value, 0]
                il.Emit(OpCodes.Ceq); // stack: [ref result, value == 0]
                il.Emit(OpCodes.Ldc_I4_1); // stack: [ref result, value == 0, 1]
                il.Emit(OpCodes.Xor); // stack: [ref result, value != 0]
            }
            else
                EmitConvertValue(il, typeCode, expectedTypeCode);
            switch(expectedTypeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
                il.Emit(OpCodes.Stind_I1); // result = value
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                il.Emit(OpCodes.Stind_I2); // result = value
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Emit(OpCodes.Stind_I4); // result = value
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Emit(OpCodes.Stind_I8); // result = value
                break;
            case GroBufTypeCode.Single:
                il.Emit(OpCodes.Stind_R4); // result = value
                break;
            case GroBufTypeCode.Double:
                il.Emit(OpCodes.Stind_R8); // result = value
                break;
            default:
                throw new NotSupportedException();
            }
            il.Emit(OpCodes.Ret);
            return method;
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
                throw new NotSupportedException("Type with type code '" + expectedTypeCode + "' is not supported");
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
                throw new NotSupportedException("Type with type code '" + typeCode + "' is not supported");
            }
        }
    }
}