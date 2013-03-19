using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

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

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override unsafe void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCodeMap.GetTypeCode(Type));

            var il = context.Il;
            var size = il.DeclareLocal(typeof(int));

            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [data length]
            il.Dup(); // stack: [data length, data length]
            il.Stloc(size); // size = data length; stack: [data length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [data length]
            context.AssertLength();

            var length = context.Length;
            il.Ldloc(size); // stack: [size]
            CountArrayLength(elementType, il); // stack: [array length]
            il.Stloc(length); // length = array length

            var createArrayLabel = il.DefineLabel("createArray");
            context.LoadResult(Type); // stack: [result]
            il.Brfalse(createArrayLabel); // if(result == null) goto createArray;
            context.LoadResult(Type); // stack: [result]
            il.Ldlen(); // stack: [result.Length]
            il.Ldloc(length); // stack: [result.Length, length]

            var arrayCreatedLabel = il.DefineLabel("arrayCreated");
            il.Bge(typeof(int), arrayCreatedLabel); // if(result.Length >= length) goto arrayCreated;

            context.LoadResultByRef(); // stack: [ref result]
            il.Ldloc(length); // stack: [ref result, length]
            il.Call(resizeMethod.MakeGenericMethod(elementType)); // Array.Resize(ref result, length)
            il.Br(arrayCreatedLabel); // goto arrayCreated

            il.MarkLabel(createArrayLabel);
            context.LoadResultByRef(); // stack: [ref result]
            il.Ldloc(length); // stack: [ref result, length]
            il.Newarr(elementType); // stack: [ref result, new type[length]]
            il.Stind(Type); // result = new type[length]; stack: []

            il.MarkLabel(arrayCreatedLabel);

            il.Ldloc(length);
            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(length == 0) goto allDone; stack: []

            var arr = il.DeclareLocal(elementType.MakeByRefType(), true);
            context.LoadResult(Type); // stack: [result]
            il.Ldc_I4(0); // stack: [result, 0]
            il.Ldelema(elementType); // stack: [&result[0]]
            il.Stloc(arr); // arr = &result[0]; stack: []
            il.Ldloc(arr); // stack: [arr]
            context.GoToCurrentLocation(); // stack: [arr, &data[index]]
            il.Ldloc(length); // stack: [arr, &data[index], length]
            CountArraySize(elementType, il); // stack: [arr, &data[index], size]
            if(sizeof(IntPtr) == 8)
                il.Unaligned(1L);
            il.Cpblk(); // arr = &data[index]
            il.Ldc_I4(0); // stack: [0]
            il.Conv_U(); // stack: [(uint)0]
            il.Stloc(arr); // arr = (uint)0;
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(size); // stack: [ref index, index, size]
            il.Add(); // stack: [ref index, index + size]
            il.Stind(typeof(int)); // index = index + size

            il.MarkLabel(doneLabel); // stack: []
        }

        private static void CountArraySize(Type elementType, GroboIL il)
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
                il.Ldc_I4(1);
                il.Shl();
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Ldc_I4(2);
                il.Shl();
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Ldc_I4(3);
                il.Shl();
                break;
            case GroBufTypeCode.Single:
                il.Ldc_I4(2);
                il.Shl();
                break;
            case GroBufTypeCode.Double:
                il.Ldc_I4(3);
                il.Shl();
                break;
            default:
                throw new NotSupportedException("Type '" + elementType + "' is not supported");
            }
        }

        private static void CountArrayLength(Type elementType, GroboIL il)
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
                il.Ldc_I4(1);
                il.Shr(typeof(int));
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Ldc_I4(2);
                il.Shr(typeof(int));
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Ldc_I4(3);
                il.Shr(typeof(int));
                break;
            case GroBufTypeCode.Single:
                il.Ldc_I4(2);
                il.Shr(typeof(int));
                break;
            case GroBufTypeCode.Double:
                il.Ldc_I4(3);
                il.Shr(typeof(int));
                break;
            default:
                throw new NotSupportedException("Type '" + elementType + "' is not supported");
            }
        }

        private static readonly MethodInfo resizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 0))).Body).Method.GetGenericMethodDefinition();
        private readonly Type elementType;
    }
}