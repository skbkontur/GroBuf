using System;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class ArrayReaderBuilder<T> : ReaderBuilderBase<T>
    {
        public ArrayReaderBuilder()
        {
            if(!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
            if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext<T> context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCode.Array);

            var il = context.Il;
            var length = context.Length;

            il.Emit(OpCodes.Ldc_I4_4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [data length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [data length]

            context.AssertLength();
            il.Emit(OpCodes.Ldc_I4_4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [array length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [array length]
            il.Emit(OpCodes.Stloc, length); // length = array length; stack: []

            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            var elementType = Type.GetElementType();
            il.Emit(OpCodes.Newarr, elementType); // stack: [new type[length] = result]
            il.Emit(OpCodes.Ldloc, length); // stack: [result, length]
            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, allDoneLabel); // if(length == 0) goto allDone; stack: [result]
            var i = il.DeclareLocal(typeof(uint));
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Stloc, i); // i = 0; stack: [result]
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);
            il.Emit(OpCodes.Dup); // stack: [result, result]
            il.Emit(OpCodes.Ldloc, i); // stack: [result, result, i]

            if(elementType.IsValueType && !elementType.IsPrimitive) // struct
                il.Emit(OpCodes.Ldelema, elementType);

            context.LoadData(); // stack: [result, {result[i]}, pinnedData]
            context.LoadIndexByRef(); // stack: [result, {result[i]}, pinnedData, ref index]
            context.LoadDataLength(); // stack: [result, {result[i]}, pinnedData, ref index, dataLength]
            il.Emit(OpCodes.Call, context.Context.GetReader(elementType)); // reader(pinnedData, ref index, dataLength); stack: [result, {result[i]}, item]
            EmitArrayItemSetter(elementType, il); // result[i] = item; stack: [result]
            il.Emit(OpCodes.Ldloc, i); // stack: [result, i]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [result, i, 1]
            il.Emit(OpCodes.Add); // stack: [result, i + 1]
            il.Emit(OpCodes.Dup); // stack: [result, i + 1, i + 1]
            il.Emit(OpCodes.Stloc, i); // i = i + 1; stack: [result, i]
            il.Emit(OpCodes.Ldloc, length); // stack: [result, i, length]
            il.Emit(OpCodes.Blt_Un, cycleStart); // if(i < length) goto cycleStart
            il.MarkLabel(allDoneLabel); // stack: [result]
        }

        private static void EmitArrayItemSetter(Type elementType, ILGenerator il)
        {
            if(elementType.IsClass) // class
                il.Emit(OpCodes.Stelem_Ref);
            else if(!elementType.IsPrimitive) // struct
                il.Emit(OpCodes.Stobj, elementType);
            else
            {
                // Primitive
                switch(Type.GetTypeCode(elementType))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    il.Emit(OpCodes.Stelem_I1);
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Char:
                    il.Emit(OpCodes.Stelem_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Stelem_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Stelem_I8);
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Stelem_R4);
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Stelem_R8);
                    break;
                default:
                    throw new NotSupportedException("Type '" + elementType + "' is not supported");
                }
            }
        }
    }
}