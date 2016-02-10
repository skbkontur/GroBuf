using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class PrimitivesArraySegmentWriterBuilder : WriterBuilderBase
    {
        public PrimitivesArraySegmentWriterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(ArraySegment<>)))
                throw new InvalidOperationException("An array segment expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
            arrayField = Type.GetField("_array", BindingFlags.Instance | BindingFlags.NonPublic);
            offsetField = Type.GetField("_offset", BindingFlags.Instance | BindingFlags.NonPublic);
            countField = Type.GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!elementType.IsPrimitive)
                throw new NotSupportedException("Array segment of primitive type expected but was '" + Type + "'");
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            context.LoadObjByRef(); // stack: [ref obj]
            il.Ldfld(arrayField); // stack: [obj._array]
            if(context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
                il.Brtrue(notEmptyLabel); // if(obj._array != null) goto notEmpty;
            else
            {
                var emptyLabel = il.DefineLabel("empty");
                il.Brfalse(emptyLabel); // if(obj._array == null) goto empty;
                context.LoadObjByRef(); // stack: [ref obj]
                il.Ldfld(countField); // stack: [obj._count]
                il.Brtrue(notEmptyLabel); // if(obj._count != 0) goto notEmpty;
                il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override bool IsReference { get { return false; } }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override unsafe void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            var typeCode = GroBufTypeCodeMap.GetTypeCode(Type);
            context.WriteTypeCode(typeCode);
            var size = il.DeclareLocal(typeof(int));

            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], ref obj]
            il.Ldfld(countField); // stack: [&result[index], obj._count]
            CountArraySize(elementType, il); // stack: [&result[index], obj size]
            il.Dup(); // stack: [&result[index], obj size, obj size]
            il.Stloc(size); // size = obj size; stack: [&result[index], obj size]
            il.Stind(typeof(int)); // result[index] = size; stack: []
            context.IncreaseIndexBy4(); // index = index + 4; stack: []

            var doneLabel = il.DefineLabel("done");
            il.Ldloc(size); // stack: [size]
            il.Brfalse(doneLabel); // if(size == 0) goto done; stack: []

            il.Ldloc(size);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], ref obj]
            il.Ldfld(arrayField); // stack: [&result[index], obj._array]
            context.LoadObjByRef(); // stack: [&result[index], obj._array, ref obj]
            il.Ldfld(offsetField); // stack: [&result[index], obj._array, obj._offset]
            il.Ldelema(elementType); // stack: [&result[index], &obj._array[obj._offset]]
            var arr = il.DeclareLocal(elementType.MakeByRefType(), true);
            il.Stloc(arr); // arr = &obj._array[obj._offset]; stack: [&result[index]]
            il.Ldloc(arr); // stack: [&result[index], arr]
            il.Ldloc(size); // stack: [&result[index], arr + obj._offset, size]
            il.Cpblk(); // &result[index] = arr
            il.Ldnull(); // stack: [null]
            il.Stloc(arr); // arr = null;
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(size); // stack: [ref index, index, size]
            il.Add(); // stack: [ref index, index + size]
            il.Stind(typeof(int)); // index = index + size
            il.MarkLabel(doneLabel);
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

        private readonly Type elementType;
        private readonly FieldInfo arrayField;
        private readonly FieldInfo offsetField;
        private readonly FieldInfo countField;
    }
}