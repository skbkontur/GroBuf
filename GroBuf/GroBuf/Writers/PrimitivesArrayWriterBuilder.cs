using System;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class PrimitivesArrayWriterBuilder<T> : WriterBuilderBase<T>
    {
        public PrimitivesArrayWriterBuilder()
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

        protected override bool CheckEmpty(WriterMethodBuilderContext context, Label notEmptyLabel)
        {
            var emptyLabel = context.Il.DefineLabel();
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Brfalse, emptyLabel); // if(obj == null) goto empty;
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Ldlen); // stack: [obj.Length]
            context.Il.Emit(OpCodes.Brtrue, notEmptyLabel); // if(obj.Length != 0) goto notEmpty;
            context.Il.MarkLabel(emptyLabel);
            return true;
        }

        protected override unsafe void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            var typeCode = GroBufTypeCodeMap.GetTypeCode(Type);
            context.WriteTypeCode(typeCode);
            var size = il.DeclareLocal(typeof(int));
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Emit(OpCodes.Ldlen); // stack: [&result[index], obj.Length]
            CountArraySize(elementType, il); // stack: [&result[index], obj size]
            il.Emit(OpCodes.Dup); // stack: [&result[index], obj size, obj size]
            il.Emit(OpCodes.Stloc, size); // size = obj size; stack: [&result[index], obj size]
            il.Emit(OpCodes.Stind_I4); // result[index] = size; stack: []
            context.IncreaseIndexBy4(); // index = index + 4; stack: []
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [&result[index], obj, 0]
            il.Emit(OpCodes.Ldelema, elementType); // stack: [&result[index], &obj[0]]
            var arr = il.DeclareLocal(elementType.MakeByRefType(), true);
            il.Emit(OpCodes.Stloc, arr); // arr = &obj[0]; stack: [&result[index]]
            il.Emit(OpCodes.Ldloc, arr); // stack: [&result[index], arr]
            il.Emit(OpCodes.Ldloc, size); // stack: [&result[index], arr, size]
            if(sizeof(IntPtr) == 8)
                context.Il.Emit(OpCodes.Unaligned, 1L);
            context.Il.Emit(OpCodes.Cpblk); // &result[index] = arr
            context.Il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            context.Il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            context.Il.Emit(OpCodes.Stloc, arr); // arr = (uint)0;
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Emit(OpCodes.Ldloc, size); // stack: [ref index, index, size]
            il.Emit(OpCodes.Add); // stack: [ref index, index + size]
            il.Emit(OpCodes.Stind_I4); // index = index + size
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

        private readonly Type elementType;
    }
}