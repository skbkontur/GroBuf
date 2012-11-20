using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class PrimitivesArrayReaderBuilder : ReaderBuilderBase
    {
        public PrimitivesArrayReaderBuilder(Type type)
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

        protected override unsafe void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCodeMap.GetTypeCode(Type));

            var il = context.Il;
            var size = il.DeclareLocal(typeof(int));

            il.Emit(OpCodes.Ldc_I4_4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [data length]
            il.Emit(OpCodes.Dup); // stack: [data length, data length]
            il.Emit(OpCodes.Stloc, size); // size = data length; stack: [data length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [data length]
            context.AssertLength();

            var length = context.Length;
            il.Emit(OpCodes.Ldloc, size); // stack: [size]
            CountArrayLength(elementType, il); // stack: [array length]
            il.Emit(OpCodes.Stloc, length); // length = array length

            var createArrayLabel = il.DefineLabel();
            context.LoadResult(); // stack: [result]
            il.Emit(OpCodes.Brfalse, createArrayLabel); // if(result == null) goto createArray;
            context.LoadResult(); // stack: [result]
            il.Emit(OpCodes.Ldlen); // stack: [result.Length]
            il.Emit(OpCodes.Ldloc, length); // stack: [result.Length, length]

            var arrayCreatedLabel = il.DefineLabel();
            il.Emit(OpCodes.Bge, arrayCreatedLabel); // if(result.Length >= length) goto arrayCreated;

            context.LoadResultByRef(); // stack: [ref result]
            il.Emit(OpCodes.Ldloc, length); // stack: [ref result, length]
            il.Emit(OpCodes.Call, resizeMethod.MakeGenericMethod(elementType)); // Array.Resize(ref result, length)
            il.Emit(OpCodes.Br, arrayCreatedLabel); // goto arrayCreated

            il.MarkLabel(createArrayLabel);
            context.LoadResultByRef(); // stack: [ref result]
            il.Emit(OpCodes.Ldloc, length); // stack: [ref result, length]
            il.Emit(OpCodes.Newarr, elementType); // stack: [ref result, new type[length]]
            il.Emit(OpCodes.Stind_Ref); // result = new type[length]; stack: []

            il.MarkLabel(arrayCreatedLabel);

            il.Emit(OpCodes.Ldloc, length);
            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, allDoneLabel); // if(length == 0) goto allDone; stack: []

            var arr = il.DeclareLocal(elementType.MakeByRefType(), true);
            context.LoadResult(); // stack: [result]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Ldelema, elementType); // stack: [&result[0]]
            il.Emit(OpCodes.Stloc, arr); // arr = &result[0]; stack: []
            il.Emit(OpCodes.Ldloc, arr); // stack: [arr]
            context.GoToCurrentLocation(); // stack: [arr, &data[index]]
            il.Emit(OpCodes.Ldloc, length); // stack: [arr, &data[index], length]
            CountArraySize(elementType, il); // stack: [arr, &data[index], size]
            if(sizeof(IntPtr) == 8)
                context.Il.Emit(OpCodes.Unaligned, 1L);
            context.Il.Emit(OpCodes.Cpblk); // arr = &data[index]
            context.Il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            context.Il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            context.Il.Emit(OpCodes.Stloc, arr); // arr = (uint)0;
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Emit(OpCodes.Ldloc, size); // stack: [ref index, index, size]
            il.Emit(OpCodes.Add); // stack: [ref index, index + size]
            il.Emit(OpCodes.Stind_I4); // index = index + size

            il.MarkLabel(allDoneLabel); // stack: []
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

        private static void CountArrayLength(Type elementType, ILGenerator il)
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
                il.Emit(OpCodes.Shr);
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Emit(OpCodes.Ldc_I4_2);
                il.Emit(OpCodes.Shr);
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Emit(OpCodes.Ldc_I4_3);
                il.Emit(OpCodes.Shr);
                break;
            case GroBufTypeCode.Single:
                il.Emit(OpCodes.Ldc_I4_2);
                il.Emit(OpCodes.Shr);
                break;
            case GroBufTypeCode.Double:
                il.Emit(OpCodes.Ldc_I4_3);
                il.Emit(OpCodes.Shr);
                break;
            default:
                throw new NotSupportedException("Type '" + elementType + "' is not supported");
            }
        }

        private static readonly MethodInfo resizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 0))).Body).Method.GetGenericMethodDefinition();
        private readonly Type elementType;
    }
}