using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class PrimitivesReaderBuilder : ReaderBuilderBase
    {
        public PrimitivesReaderBuilder(Type type)
            : base(type)
        {
            if(!Type.IsPrimitive && Type != typeof(decimal)) throw new InvalidOperationException("Expected primitive type but was '" + Type + "'");
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
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
                    GroBufTypeCode.Single, GroBufTypeCode.Double,
                    GroBufTypeCode.Boolean, GroBufTypeCode.DateTime,
                    GroBufTypeCode.Decimal
                })
                result[(int)typeCode] = BuildPrimitiveValueReader(context, typeCode);
            return result;
        }

        // todo: kill
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
            var expectedTypeCode = GroBufTypeCodeMap.GetTypeCode(Type);

            il.Emit(OpCodes.Ldarg_1); // stack: [ref result]
            if (typeCode == GroBufTypeCode.Decimal)
            {
                if (expectedTypeCode == GroBufTypeCode.Boolean)
                {
                    il.Emit(OpCodes.Ldarg_0); // stack: [ref result, &temp, address]
                    il.Emit(OpCodes.Ldind_I8); // stack: [ref result, &temp, (long)*address]
                    il.Emit(OpCodes.Ldarg_0); // stack: [ref result, &temp + 8, address]
                    il.Emit(OpCodes.Ldc_I4_8); // stack: [ref result, &temp + 8, address, 8]
                    il.Emit(OpCodes.Add); // stack: [ref result, &temp + 8, address + 8]
                    il.Emit(OpCodes.Ldind_I8); // stack: [ref result, &temp + 8, (long)*(address + 8)]
                    il.Emit(OpCodes.Or);
                    il.Emit(OpCodes.Ldc_I4_0); // stack: [ref result, value, 0]
                    il.Emit(OpCodes.Ceq); // stack: [ref result, value == 0]
                    il.Emit(OpCodes.Ldc_I4_1); // stack: [ref result, value == 0, 1]
                    il.Emit(OpCodes.Xor); // stack: [ref result, value != 0]
                }
                else
                {
                    var temp = il.DeclareLocal(typeof(decimal));
                    il.Emit(OpCodes.Ldloca_S, temp); // stack: [ref result, &temp]
                    il.Emit(OpCodes.Ldarg_0); // stack: [ref result, &temp, address]
                    il.Emit(OpCodes.Ldind_I8); // stack: [ref result, &temp, (long)*address]
                    il.Emit(OpCodes.Stind_I8); // *temp = *address;
                    il.Emit(OpCodes.Ldloca_S, temp); // stack: [ref result, &temp]
                    il.Emit(OpCodes.Ldc_I4_8); // stack: [ref result, &temp, 8]
                    il.Emit(OpCodes.Add); // stack: [ref result, &temp + 8]
                    il.Emit(OpCodes.Ldarg_0); // stack: [ref result, &temp + 8, address]
                    il.Emit(OpCodes.Ldc_I4_8); // stack: [ref result, &temp + 8, address, 8]
                    il.Emit(OpCodes.Add); // stack: [ref result, &temp + 8, address + 8]
                    il.Emit(OpCodes.Ldind_I8); // stack: [ref result, &temp + 8, (long)*(address + 8)]
                    il.Emit(OpCodes.Stind_I8); // *(temp + 8) = *(address + 8);

                    il.Emit(OpCodes.Ldloc, temp); // stack: [ref result, ref temp]
                    switch(expectedTypeCode)
                    {
                    case GroBufTypeCode.Int8:
                        il.EmitCall(OpCodes.Call, decimalToInt8Method, null); // stack: [ref result, (sbyte)temp]
                        break;
                    case GroBufTypeCode.UInt8:
                        il.EmitCall(OpCodes.Call, decimalToUInt8Method, null); // stack: [ref result, (byte)temp]
                        break;
                    case GroBufTypeCode.Int16:
                        il.EmitCall(OpCodes.Call, decimalToInt16Method, null); // stack: [ref result, (short)temp]
                        break;
                    case GroBufTypeCode.UInt16:
                        il.EmitCall(OpCodes.Call, decimalToUInt16Method, null); // stack: [ref result, (ushort)temp]
                        break;
                    case GroBufTypeCode.Int32:
                        il.EmitCall(OpCodes.Call, decimalToInt32Method, null); // stack: [ref result, (int)temp]
                        break;
                    case GroBufTypeCode.UInt32:
                        il.EmitCall(OpCodes.Call, decimalToUInt32Method, null); // stack: [ref result, (uint)temp]
                        break;
                    case GroBufTypeCode.Int64:
                        il.EmitCall(OpCodes.Call, decimalToInt64Method, null); // stack: [ref result, (long)temp]
                        break;
                    case GroBufTypeCode.UInt64:
                        il.EmitCall(OpCodes.Call, decimalToUInt64Method, null); // stack: [ref result, (ulong)temp]
                        break;
                    case GroBufTypeCode.Single:
                        il.EmitCall(OpCodes.Call, decimalToSingleMethod, null); // stack: [ref result, (float)temp]
                        break;
                    case GroBufTypeCode.Double:
                        il.EmitCall(OpCodes.Call, decimalToDoubleMethod, null); // stack: [ref result, (double)temp]
                        break;
                    case GroBufTypeCode.Decimal:
                        break;
                    default:
                        throw new NotSupportedException("Type with type code '" + expectedTypeCode + "' is not supported");
                    }
                }
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0); // stack: [ref result, address]
                EmitReadPrimitiveValue(il, Type == typeof(bool) ? GetTypeCodeForBool(typeCode) : typeCode); // stack: [ref result, value]
                if(Type == typeof(bool))
                {
                    il.Emit(OpCodes.Ldc_I4_0); // stack: [ref result, value, 0]
                    il.Emit(OpCodes.Ceq); // stack: [ref result, value == 0]
                    il.Emit(OpCodes.Ldc_I4_1); // stack: [ref result, value == 0, 1]
                    il.Emit(OpCodes.Xor); // stack: [ref result, value != 0]
                }
                else
                    EmitConvertValue(il, typeCode, expectedTypeCode);
            }
            switch(expectedTypeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
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
            case GroBufTypeCode.Decimal:
                il.Emit(OpCodes.Stobj, typeof(decimal)); // result = value
                break;
            default:
                throw new NotSupportedException("Type with type code '" + expectedTypeCode + "' is not supported");
            }
            il.Emit(OpCodes.Ret);
            return method;
        }

        private GroBufTypeCode GetTypeCodeForBool(GroBufTypeCode typeCode)
        {
            if(typeCode == GroBufTypeCode.Single)
                return GroBufTypeCode.Int32;
            if(typeCode == GroBufTypeCode.Double)
                return GroBufTypeCode.Int64;
            return typeCode;
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
            case GroBufTypeCode.Boolean:
                il.Emit(OpCodes.Conv_U1);
                break;
            case GroBufTypeCode.Int16:
                il.Emit(OpCodes.Conv_I2);
                break;
            case GroBufTypeCode.UInt16:
                il.Emit(OpCodes.Conv_U2);
                break;
            case GroBufTypeCode.Int32:
                if(typeCode == GroBufTypeCode.Int64 || typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.Double || typeCode == GroBufTypeCode.Single || typeCode == GroBufTypeCode.DateTime)
                    il.Emit(OpCodes.Conv_I4);
                break;
            case GroBufTypeCode.UInt32:
                if(typeCode == GroBufTypeCode.Int64 || typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.Double || typeCode == GroBufTypeCode.Single || typeCode == GroBufTypeCode.DateTime)
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
                if(typeCode != GroBufTypeCode.Int64 && typeCode != GroBufTypeCode.DateTime)
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
            case GroBufTypeCode.Decimal:
                switch(typeCode)
                {
                case GroBufTypeCode.Boolean:
                case GroBufTypeCode.Int8:
                case GroBufTypeCode.Int16:
                case GroBufTypeCode.Int32:
                case GroBufTypeCode.UInt8:
                case GroBufTypeCode.UInt16:
                    il.Emit(OpCodes.Newobj, decimalByIntConstructor);
                    break;
                case GroBufTypeCode.UInt32:
                    il.Emit(OpCodes.Newobj, decimalByUIntConstructor);
                    break;
                case GroBufTypeCode.Int64:
                case GroBufTypeCode.DateTime:
                    il.Emit(OpCodes.Newobj, decimalByLongConstructor);
                    break;
                case GroBufTypeCode.UInt64:
                    il.Emit(OpCodes.Newobj, decimalByULongConstructor);
                    break;
                case GroBufTypeCode.Single:
                    il.Emit(OpCodes.Newobj, decimalByFloatConstructor);
                    break;
                case GroBufTypeCode.Double:
                    il.Emit(OpCodes.Newobj, decimalByDoubleConstructor);
                    break;
                default:
                    throw new NotSupportedException("Type with type code '" + typeCode + "' is not supported");
                }
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
            case GroBufTypeCode.Boolean:
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
            case GroBufTypeCode.DateTime:
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

        private static readonly ConstructorInfo decimalByIntConstructor = ((NewExpression)((Expression<Func<int, decimal>>)(i => new decimal(i))).Body).Constructor;
        private static readonly ConstructorInfo decimalByUIntConstructor = ((NewExpression)((Expression<Func<uint, decimal>>)(i => new decimal(i))).Body).Constructor;
        private static readonly ConstructorInfo decimalByLongConstructor = ((NewExpression)((Expression<Func<long, decimal>>)(i => new decimal(i))).Body).Constructor;
        private static readonly ConstructorInfo decimalByULongConstructor = ((NewExpression)((Expression<Func<ulong, decimal>>)(i => new decimal(i))).Body).Constructor;
        private static readonly ConstructorInfo decimalByFloatConstructor = ((NewExpression)((Expression<Func<float, decimal>>)(i => new decimal(i))).Body).Constructor;
        private static readonly ConstructorInfo decimalByDoubleConstructor = ((NewExpression)((Expression<Func<double, decimal>>)(i => new decimal(i))).Body).Constructor;

        private static readonly MethodInfo decimalToInt8Method = ((UnaryExpression)((Expression<Func<decimal, sbyte>>)(d => (sbyte)d)).Body).Method;
        private static readonly MethodInfo decimalToUInt8Method = ((UnaryExpression)((Expression<Func<decimal, byte>>)(d => (byte)d)).Body).Method;
        private static readonly MethodInfo decimalToInt16Method = ((UnaryExpression)((Expression<Func<decimal, short>>)(d => (short)d)).Body).Method;
        private static readonly MethodInfo decimalToUInt16Method = ((UnaryExpression)((Expression<Func<decimal, ushort>>)(d => (ushort)d)).Body).Method;
        private static readonly MethodInfo decimalToInt32Method = ((UnaryExpression)((Expression<Func<decimal, int>>)(d => (int)d)).Body).Method;
        private static readonly MethodInfo decimalToUInt32Method = ((UnaryExpression)((Expression<Func<decimal, uint>>)(d => (uint)d)).Body).Method;
        private static readonly MethodInfo decimalToInt64Method = ((UnaryExpression)((Expression<Func<decimal, long>>)(d => (long)d)).Body).Method;
        private static readonly MethodInfo decimalToUInt64Method = ((UnaryExpression)((Expression<Func<decimal, ulong>>)(d => (ulong)d)).Body).Method;
        private static readonly MethodInfo decimalToSingleMethod = ((UnaryExpression)((Expression<Func<decimal, float>>)(d => (float)d)).Body).Method;
        private static readonly MethodInfo decimalToDoubleMethod = ((UnaryExpression)((Expression<Func<decimal, double>>)(d => (double)d)).Body).Method;
    }
}