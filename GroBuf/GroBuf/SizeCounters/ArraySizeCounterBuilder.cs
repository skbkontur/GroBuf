using System;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class ArraySizeCounterBuilder<T> : SizeCounterBuilderBase<T>
    {
        public ArraySizeCounterBuilder()
        {
            if(Type != typeof(Array))
            {
                if(!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
                if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
                elementType = Type.GetElementType();
            }
            else elementType = typeof(object);
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Emit(OpCodes.Ldc_I4, 9); // stack: [9 = size] 9 = type code + data length + array length

            var length = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [9, obj]
            il.Emit(OpCodes.Ldlen); // stack: [9, obj.Length]
            il.Emit(OpCodes.Dup); // stack: [9, obj.Length, obj.Length]
            il.Emit(OpCodes.Stloc, length); // length = obj.Length; stack: [9, length]

            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, allDoneLabel);

            var i = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Ldc_I4_0); // stack: [9, 0]
            il.Emit(OpCodes.Stloc, i); // i = 0; stack: [9]
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);
            context.LoadObj(); // stack: [size, obj]
            il.Emit(OpCodes.Ldloc, i); // stack: [size, obj, i]
            LoadArrayElement(elementType, il); // stack: [size, obj[i]]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [size, obj[i], true]
            il.Emit(OpCodes.Call, context.Context.GetCounter(elementType)); // stack: [size, writer(obj[i], true) = itemSize]
            il.Emit(OpCodes.Add); // stack: [size + itemSize]
            il.Emit(OpCodes.Ldloc, length); // stack: [size, length]
            il.Emit(OpCodes.Ldloc, i); // stack: [size, length, i]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [size, length, i, 1]
            il.Emit(OpCodes.Add); // stack: [size, length, i + 1]
            il.Emit(OpCodes.Dup); // stack: [size, length, i + 1, i + 1]
            il.Emit(OpCodes.Stloc, i); // i = i + 1; stack: [size, length, i]
            il.Emit(OpCodes.Bgt, cycleStart); // if(length > i) goto cycleStart; stack: [size]

            il.MarkLabel(allDoneLabel);
        }

        private static void LoadArrayElement(Type elementType, ILGenerator il)
        {
            if(elementType.IsClass) // class
                il.Emit(OpCodes.Ldelem_Ref);
            else if(!elementType.IsPrimitive)
            {
                // struct
                il.Emit(OpCodes.Ldelema, elementType);
                il.Emit(OpCodes.Ldobj, elementType);
            }
            else
            {
                // Primitive
                switch(Type.GetTypeCode(elementType))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                    il.Emit(OpCodes.Ldelem_I1);
                    break;
                case TypeCode.Byte:
                    il.Emit(OpCodes.Ldelem_U1);
                    break;
                case TypeCode.Int16:
                    il.Emit(OpCodes.Ldelem_I2);
                    break;
                case TypeCode.Int32:
                    il.Emit(OpCodes.Ldelem_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Ldelem_I8);
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Ldelem_U2);
                    break;
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Ldelem_U4);
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Ldelem_R4);
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Ldelem_R8);
                    break;
                default:
                    throw new NotSupportedException("Type '" + elementType + "' is not supported");
                }
            }
        }

        private readonly Type elementType;
    }
}