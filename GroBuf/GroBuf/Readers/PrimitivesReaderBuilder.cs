using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

using System.Linq;

namespace GroBuf.Readers
{
    internal class PrimitivesReaderBuilder : ReaderBuilderBase
    {
        public PrimitivesReaderBuilder(Type type)
            : base(type)
        {
            if(!Type.IsPrimitive && Type != typeof(decimal)) throw new InvalidOperationException("Expected primitive type but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("readers_" + Type.Name + "_" + Guid.NewGuid(), typeof(IntPtr[])),
                    new KeyValuePair<string, Type>("delegates_" + Type.Name + "_" + Guid.NewGuid(), typeof(Delegate[])),
                });
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1();
            var readers = BuildPrimitiveValueReaders(context.Context);
            var readersField = context.Context.InitConstField(Type, 0, readers.Select(pair => pair.Value).ToArray());
            context.Context.InitConstField(Type, 1, readers.Select(pair => pair.Key).ToArray());
            var il = context.Il;

            context.GoToCurrentLocation(); // stack: [&data[index]]
            context.LoadResultByRef(); // stack: [&data[index], ref result]
            context.SkipValue();
            context.LoadField(readersField); // stack: [&data[index], ref result, readers]
            il.Ldloc(context.TypeCode); // stack: [&data[index], ref result, readers, typeCode]
            il.Ldelem(typeof(IntPtr)); // stack: [&data[index], ref result, readers[typeCode]]
            il.Calli(CallingConventions.Standard, typeof(void), new[] {typeof(byte*), Type.MakeByRefType()}); // readers[typeCode](&data[index], ref result); stack: []
        }

        private delegate void PrimitiveValueReaderDelegate<T>(IntPtr data, ref T result);

        private KeyValuePair<Delegate, IntPtr>[] BuildPrimitiveValueReaders(ReaderTypeBuilderContext context)
        {
            var result = new KeyValuePair<Delegate, IntPtr>[256];
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
        private KeyValuePair<Delegate, IntPtr> BuildDefaultValueReader(ReaderTypeBuilderContext context)
        {
            var method = new DynamicMethod("Default_" + Type.Name + "_" + Guid.NewGuid(), typeof(void), new[] {typeof(IntPtr), Type.MakeByRefType()}, context.Module, true);
            var il = new GroboIL(method);
            il.Ldarg(1); // stack: [ref result]
            il.Initobj(Type); // [result = default(T)]
            il.Ret();
            var @delegate = method.CreateDelegate(typeof(PrimitiveValueReaderDelegate<>).MakeGenericType(Type));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }

        private KeyValuePair<Delegate, IntPtr> BuildPrimitiveValueReader(ReaderTypeBuilderContext context, GroBufTypeCode typeCode)
        {
            var method = new DynamicMethod("Read_" + Type.Name + "_from_" + typeCode + "_" + Guid.NewGuid(), typeof(void), new[] {typeof(IntPtr), Type.MakeByRefType()}, context.Module, true);
            var il = new GroboIL(method);
            var expectedTypeCode = GroBufTypeCodeMap.GetTypeCode(Type);

            il.Ldarg(1); // stack: [ref result]
            if(typeCode == GroBufTypeCode.Decimal)
            {
                if(expectedTypeCode == GroBufTypeCode.Boolean)
                {
                    il.Ldarg(0); // stack: [ref result, &temp, address]
                    il.Ldind(typeof(long)); // stack: [ref result, &temp, (long)*address]
                    il.Ldarg(0); // stack: [ref result, &temp + 8, address]
                    il.Ldc_I4(8); // stack: [ref result, &temp + 8, address, 8]
                    il.Add(); // stack: [ref result, &temp + 8, address + 8]
                    il.Ldind(typeof(long)); // stack: [ref result, &temp + 8, (long)*(address + 8)]
                    il.Or();
                    il.Ldc_I4(0); // stack: [ref result, value, 0]
                    il.Ceq(); // stack: [ref result, value == 0]
                    il.Ldc_I4(1); // stack: [ref result, value == 0, 1]
                    il.Xor(); // stack: [ref result, value != 0]
                }
                else
                {
                    var temp = il.DeclareLocal(typeof(decimal));
                    il.Ldloca(temp); // stack: [ref result, &temp]
                    il.Ldarg(0); // stack: [ref result, &temp, address]
                    il.Ldind(typeof(long)); // stack: [ref result, &temp, (long)*address]
                    il.Stind(typeof(long)); // *temp = *address;
                    il.Ldloca(temp); // stack: [ref result, &temp]
                    il.Ldc_I4(8); // stack: [ref result, &temp, 8]
                    il.Add(); // stack: [ref result, &temp + 8]
                    il.Ldarg(0); // stack: [ref result, &temp + 8, address]
                    il.Ldc_I4(8); // stack: [ref result, &temp + 8, address, 8]
                    il.Add(); // stack: [ref result, &temp + 8, address + 8]
                    il.Ldind(typeof(long)); // stack: [ref result, &temp + 8, (long)*(address + 8)]
                    il.Stind(typeof(long)); // *(temp + 8) = *(address + 8);

                    il.Ldloc(temp); // stack: [ref result, ref temp]
                    switch(expectedTypeCode)
                    {
                    case GroBufTypeCode.Int8:
                        il.Call(decimalToInt8Method); // stack: [ref result, (sbyte)temp]
                        break;
                    case GroBufTypeCode.UInt8:
                        il.Call(decimalToUInt8Method, null); // stack: [ref result, (byte)temp]
                        break;
                    case GroBufTypeCode.Int16:
                        il.Call(decimalToInt16Method, null); // stack: [ref result, (short)temp]
                        break;
                    case GroBufTypeCode.UInt16:
                        il.Call(decimalToUInt16Method, null); // stack: [ref result, (ushort)temp]
                        break;
                    case GroBufTypeCode.Int32:
                        il.Call(decimalToInt32Method, null); // stack: [ref result, (int)temp]
                        break;
                    case GroBufTypeCode.UInt32:
                        il.Call(decimalToUInt32Method, null); // stack: [ref result, (uint)temp]
                        break;
                    case GroBufTypeCode.Int64:
                        il.Call(decimalToInt64Method, null); // stack: [ref result, (long)temp]
                        break;
                    case GroBufTypeCode.UInt64:
                        il.Call(decimalToUInt64Method, null); // stack: [ref result, (ulong)temp]
                        break;
                    case GroBufTypeCode.Single:
                        il.Call(decimalToSingleMethod, null); // stack: [ref result, (float)temp]
                        break;
                    case GroBufTypeCode.Double:
                        il.Call(decimalToDoubleMethod, null); // stack: [ref result, (double)temp]
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
                il.Ldarg(0); // stack: [ref result, address]
                EmitReadPrimitiveValue(il, Type == typeof(bool) ? GetTypeCodeForBool(typeCode) : typeCode); // stack: [ref result, value]
                if(Type == typeof(bool))
                {
                    il.Ldc_I4(0); // stack: [ref result, value, 0]
                    il.Ceq(); // stack: [ref result, value == 0]
                    il.Ldc_I4(1); // stack: [ref result, value == 0, 1]
                    il.Xor(); // stack: [ref result, value != 0]
                }
                else
                    EmitConvertValue(il, typeCode, expectedTypeCode);
            }
            switch(expectedTypeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                il.Stind(typeof(byte)); // result = value
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                il.Stind(typeof(short)); // result = value
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Stind(typeof(int)); // result = value
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Stind(typeof(long)); // result = value
                break;
            case GroBufTypeCode.Single:
                il.Stind(typeof(float)); // result = value
                break;
            case GroBufTypeCode.Double:
                il.Stind(typeof(double)); // result = value
                break;
            case GroBufTypeCode.Decimal:
                il.Stobj(typeof(decimal)); // result = value
                break;
            default:
                throw new NotSupportedException("Type with type code '" + expectedTypeCode + "' is not supported");
            }
            il.Ret();
            var @delegate = method.CreateDelegate(typeof(PrimitiveValueReaderDelegate<>).MakeGenericType(Type));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }

        private GroBufTypeCode GetTypeCodeForBool(GroBufTypeCode typeCode)
        {
            if(typeCode == GroBufTypeCode.Single)
                return GroBufTypeCode.Int32;
            if(typeCode == GroBufTypeCode.Double)
                return GroBufTypeCode.Int64;
            return typeCode;
        }

        private static void EmitConvertValue(GroboIL il, GroBufTypeCode typeCode, GroBufTypeCode expectedTypeCode)
        {
            if(expectedTypeCode == typeCode)
                return;
            switch(expectedTypeCode)
            {
            case GroBufTypeCode.Int8:
                il.Conv_I1();
                break;
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                il.Conv_U1();
                break;
            case GroBufTypeCode.Int16:
                il.Conv_I2();
                break;
            case GroBufTypeCode.UInt16:
                il.Conv_U2();
                break;
            case GroBufTypeCode.Int32:
                if(typeCode == GroBufTypeCode.Int64 || typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.Double || typeCode == GroBufTypeCode.Single || typeCode == GroBufTypeCode.DateTime)
                    il.Conv_I4();
                break;
            case GroBufTypeCode.UInt32:
                if(typeCode == GroBufTypeCode.Int64 || typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.Double || typeCode == GroBufTypeCode.Single || typeCode == GroBufTypeCode.DateTime)
                    il.Conv_U4();
                break;
            case GroBufTypeCode.Int64:
                if(typeCode != GroBufTypeCode.UInt64)
                {
                    if(typeCode == GroBufTypeCode.UInt8 || typeCode == GroBufTypeCode.UInt16 || typeCode == GroBufTypeCode.UInt32)
                        il.Conv_U8();
                    else
                        il.Conv_I8();
                }
                break;
            case GroBufTypeCode.UInt64:
                if(typeCode != GroBufTypeCode.Int64 && typeCode != GroBufTypeCode.DateTime)
                {
                    if(typeCode == GroBufTypeCode.Int8 || typeCode == GroBufTypeCode.Int16 || typeCode == GroBufTypeCode.Int32)
                        il.Conv_I8();
                    else
                        il.Conv_U8();
                }
                break;
            case GroBufTypeCode.Single:
                if(typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.UInt32)
                    il.Conv_R_Un();
                il.Conv_R4();
                break;
            case GroBufTypeCode.Double:
                if(typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.UInt32)
                    il.Conv_R_Un();
                il.Conv_R8();
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
                    il.Newobj(decimalByIntConstructor);
                    break;
                case GroBufTypeCode.UInt32:
                    il.Newobj(decimalByUIntConstructor);
                    break;
                case GroBufTypeCode.Int64:
                case GroBufTypeCode.DateTime:
                    il.Newobj(decimalByLongConstructor);
                    break;
                case GroBufTypeCode.UInt64:
                    il.Newobj(decimalByULongConstructor);
                    break;
                case GroBufTypeCode.Single:
                    il.Newobj(decimalByFloatConstructor);
                    break;
                case GroBufTypeCode.Double:
                    il.Newobj(decimalByDoubleConstructor);
                    break;
                default:
                    throw new NotSupportedException("Type with type code '" + typeCode + "' is not supported");
                }
                break;
            default:
                throw new NotSupportedException("Type with type code '" + expectedTypeCode + "' is not supported");
            }
        }

        private static void EmitReadPrimitiveValue(GroboIL il, GroBufTypeCode typeCode)
        {
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
                il.Ldind(typeof(sbyte));
                break;
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                il.Ldind(typeof(byte));
                break;
            case GroBufTypeCode.Int16:
                il.Ldind(typeof(short));
                break;
            case GroBufTypeCode.UInt16:
                il.Ldind(typeof(ushort));
                break;
            case GroBufTypeCode.Int32:
                il.Ldind(typeof(int));
                break;
            case GroBufTypeCode.UInt32:
                il.Ldind(typeof(uint));
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.DateTime:
                il.Ldind(typeof(long));
                break;
            case GroBufTypeCode.UInt64:
                il.Ldind(typeof(ulong));
                break;
            case GroBufTypeCode.Single:
                il.Ldind(typeof(float));
                break;
            case GroBufTypeCode.Double:
                il.Ldind(typeof(double));
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