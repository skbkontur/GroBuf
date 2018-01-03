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
            if(!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
            if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
            elementType = Type.GetElementType();
            if(!elementType.IsPrimitive) throw new NotSupportedException("Array of primitive type expected but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override unsafe void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;

            il.Ldloc(context.TypeCode); // stack: [type code]
            il.Ldc_I4((int)GroBufTypeCodeMap.GetTypeCode(Type)); // stack: [type code, GroBufTypeCode(Type)]
            var tryReadArrayElementLabel = il.DefineLabel("tryReadArrayElement");
            il.Bne_Un(tryReadArrayElementLabel); // if(type code != GroBufTypeCode(Type)) goto tryReadArrayElement; stack: []

            context.IncreaseIndexBy1();

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

            if(context.Context.GroBufReader.Options.HasFlag(GroBufOptions.MergeOnRead))
            {
                var createArrayLabel = il.DefineLabel("createArray");
                context.LoadResult(Type); // stack: [result]
                il.Brfalse(createArrayLabel); // if(result == null) goto createArray;
                context.LoadResult(Type); // stack: [result]
                il.Ldlen(); // stack: [result.Length]
                il.Ldloc(length); // stack: [result.Length, length]

                var arrayCreatedLabel = il.DefineLabel("arrayCreated");
                il.Bge(arrayCreatedLabel, false); // if(result.Length >= length) goto arrayCreated;

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
            }
            else
            {
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(length); // stack: [ref result, length]
                il.Newarr(elementType); // stack: [ref result, new type[length]]
                il.Stind(Type); // result = new type[length]; stack: []
            }

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
            il.Cpblk(); // arr = &data[index]
            il.FreePinnedLocal(arr); // arr = null; stack: []
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(size); // stack: [ref index, index, size]
            il.Add(); // stack: [ref index, index + size]
            il.Stind(typeof(int)); // index = index + size
            il.Br(doneLabel);

            il.MarkLabel(tryReadArrayElementLabel);
            if(context.Context.GroBufReader.Options.HasFlag(GroBufOptions.MergeOnRead))
            {
                var createArrayLabel = il.DefineLabel("createArray");
                context.LoadResult(Type); // stack: [result]
                il.Brfalse(createArrayLabel); // if(result == null) goto createArray;
                context.LoadResult(Type); // stack: [result]
                il.Ldlen(); // stack: [result.Length]
                il.Ldc_I4(1); // stack: [result.Length, 1]

                var arrayCreatedLabel = il.DefineLabel("arrayCreated");
                il.Bge(arrayCreatedLabel, false); // if(result.Length >= 1) goto arrayCreated;

                context.LoadResultByRef(); // stack: [ref result]
                il.Ldc_I4(1); // stack: [ref result, 1]
                il.Call(resizeMethod.MakeGenericMethod(elementType)); // Array.Resize(ref result, 1)
                il.Br(arrayCreatedLabel); // goto arrayCreated

                il.MarkLabel(createArrayLabel);
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldc_I4(1); // stack: [ref result, 1]
                il.Newarr(elementType); // stack: [ref result, new type[1]]
                il.Stind(Type); // result = new type[1]; stack: []

                il.MarkLabel(arrayCreatedLabel);
            }
            else
            {
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldc_I4(1); // stack: [ref result, 1]
                il.Newarr(elementType); // stack: [ref result, new type[1]]
                il.Stind(Type); // result = new type[1]; stack: []
            }

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadResult(Type); // stack: [pinnedData, ref index, result]
            il.Ldc_I4(0); // stack: [pinnedData, ref index, result, 0]

            il.Ldelema(elementType); // stack: [pinnedData, ref index, ref result[0]]
            context.LoadContext(); // stack: [pinnedData, ref index, ref result[0], context]

            context.CallReader(elementType); // reader(pinnedData, ref index, ref result[0], context); stack: []

            il.MarkLabel(doneLabel); // stack: []
        }

        protected override bool IsReference { get { return true; } }

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
                il.Shr(false);
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Ldc_I4(2);
                il.Shr(false);
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Ldc_I4(3);
                il.Shr(false);
                break;
            case GroBufTypeCode.Single:
                il.Ldc_I4(2);
                il.Shr(false);
                break;
            case GroBufTypeCode.Double:
                il.Ldc_I4(3);
                il.Shr(false);
                break;
            default:
                throw new NotSupportedException("Type '" + elementType + "' is not supported");
            }
        }

        private static readonly MethodInfo resizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 0))).Body).Method.GetGenericMethodDefinition();
        private readonly Type elementType;
    }
}