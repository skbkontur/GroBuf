using System;

using GrEmit;

namespace GroBuf.Writers
{
    internal class PrimitivesArrayWriterBuilder : WriterBuilderBase
    {
        public PrimitivesArrayWriterBuilder(Type type)
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

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            var emptyLabel = il.DefineLabel("empty");
            context.LoadObj(); // stack: [obj]
            il.Brfalse(emptyLabel); // if(obj == null) goto empty;
            context.LoadObj(); // stack: [obj]
            il.Ldlen(); // stack: [obj.Length]
            il.Brtrue(notEmptyLabel); // if(obj.Length != 0) goto notEmpty;
            il.MarkLabel(emptyLabel);
            return true;
        }

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
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Ldlen(); // stack: [&result[index], obj.Length]
            CountArraySize(elementType, il); // stack: [&result[index], obj size]
            il.Dup(); // stack: [&result[index], obj size, obj size]
            il.Stloc(size); // size = obj size; stack: [&result[index], obj size]
            il.Stind(typeof(int)); // result[index] = size; stack: []
            context.IncreaseIndexBy4(); // index = index + 4; stack: []
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Ldc_I4(0); // stack: [&result[index], obj, 0]
            il.Ldelema(elementType); // stack: [&result[index], &obj[0]]
            var arr = il.DeclareLocal(elementType.MakeByRefType(), true);
            il.Stloc(arr); // arr = &obj[0]; stack: [&result[index]]
            il.Ldloc(arr); // stack: [&result[index], arr]
            il.Ldloc(size); // stack: [&result[index], arr, size]
            if(sizeof(IntPtr) == 8)
                context.Il.Unaligned(1L);
            context.Il.Cpblk(); // &result[index] = arr
            context.Il.Ldc_I4(0); // stack: [0]
            context.Il.Conv_U(); // stack: [(uint)0]
            context.Il.Stloc(arr); // arr = (uint)0;
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(size); // stack: [ref index, index, size]
            il.Add(); // stack: [ref index, index + size]
            il.Stind(typeof(int)); // index = index + size
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
    }
}