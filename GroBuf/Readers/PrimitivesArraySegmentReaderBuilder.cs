using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.Readers
{
    internal class PrimitivesArraySegmentReaderBuilder : ReaderBuilderBase
    {
        public PrimitivesArraySegmentReaderBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(ArraySegment<>)))
                throw new InvalidOperationException("An array segment expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
            arrayField = Type.GetField("_array", BindingFlags.Instance | BindingFlags.NonPublic);
            offsetField = Type.GetField("_offset", BindingFlags.Instance | BindingFlags.NonPublic);
            countField = Type.GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic);
            if(!elementType.IsPrimitive)
                throw new NotSupportedException("Array segment of primitive type expected but was '" + Type + "'");
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

            var array = il.DeclareLocal(elementType.MakeArrayType());
            context.LoadResultByRef(); // stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Ldc_I4(0); // stack: [ref result, ref result, 0]
            il.Stfld(offsetField); // result._offset = 0; stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Ldloc(length); // stack: [ref result, ref result, length]
            il.Stfld(countField); // result._count = length; stack: [ref result]
            il.Ldloc(length); // stack: [ref result, length]
            il.Newarr(elementType); // stack: [ref result, new type[length]]
            il.Dup();
            il.Stloc(array); // array = new type[length]; stack: [ref result]
            il.Stfld(arrayField); // result._array = array; stack: []

            il.Ldloc(length);
            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(length == 0) goto allDone; stack: []

            var arr = il.DeclareLocal(elementType.MakeByRefType(), true);
            il.Ldloc(array); // stack: [array]
            il.Ldc_I4(0); // stack: [array, 0]
            il.Ldelema(elementType); // stack: [&array[0]]
            il.Stloc(arr); // arr = &array[0]; stack: []
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
            context.LoadResultByRef(); // stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Ldc_I4(0); // stack: [ref result, ref result, 0]
            il.Stfld(offsetField); // result._offset = 0; stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Ldc_I4(1); // stack: [ref result, ref result, 1]
            il.Stfld(countField); // result._count = 1; stack: [ref result]
            il.Ldc_I4(1); // stack: [ref result, 1]
            il.Newarr(elementType); // stack: [ref result, new type[1]]
            il.Dup();
            il.Stloc(array); // array = new type[1]; stack: [ref result]
            il.Stfld(arrayField); // result._array = array; stack: []

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            il.Ldloc(array); // stack: [pinnedData, ref index, array]
            il.Ldc_I4(0); // stack: [pinnedData, ref index, array, 0]

            il.Ldelema(elementType); // stack: [pinnedData, ref index, ref array[0]]
            context.LoadContext(); // stack: [pinnedData, ref index, ref array[0], context]

            context.CallReader(elementType); // reader(pinnedData, ref index, ref array[0], context); stack: []

            il.MarkLabel(doneLabel); // stack: []
        }

        protected override bool IsReference { get { return false; } }

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

        private readonly Type elementType;
        private readonly FieldInfo arrayField;
        private readonly FieldInfo offsetField;
        private readonly FieldInfo countField;
    }
}