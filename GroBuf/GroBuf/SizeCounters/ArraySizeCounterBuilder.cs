using System;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class ArraySizeCounterBuilder<T> : SizeCounterBuilderBase<T>
    {
        public ArraySizeCounterBuilder()
        {
            if(!Type.IsArray) throw new InvalidOperationException("An array expected but was " + Type);
            if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, Label notEmptyLabel)
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

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            var length = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [obj]
            il.Emit(OpCodes.Ldlen); // stack: [obj.Length]
            il.Emit(OpCodes.Stloc, length); // length = obj.Length
            il.Emit(OpCodes.Ldc_I4, 9); // stack: [9]

            var i = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Stloc, i); // i = 0; stack: []
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);
            context.LoadObj(); // stack: [obj]
            il.Emit(OpCodes.Ldloc, i); // stack: [obj, i]
            var elementType = Type.GetElementType();
            LoadArrayElement(elementType, il); // stack: [obj[i]]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [obj[i], true]
            il.Emit(OpCodes.Call, context.Context.GetCounter(elementType)); // stack: [sum, writer(obj[i], true, ref result, ref index, ref pinnedResult)]
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            il.Emit(OpCodes.Ldloc, i); // stack: [length, i]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [length, i, 1]
            il.Emit(OpCodes.Add); // stack: [length, i + 1]
            il.Emit(OpCodes.Dup); // stack: [length, i + 1, i + 1]
            il.Emit(OpCodes.Stloc, i); // i = i + 1; stack: [length, i]
            il.Emit(OpCodes.Bgt, cycleStart); // if(length > i) goto cycleStart; stack: []
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
                    throw new NotSupportedException("Type " + elementType.Name + " is not supported");
                }
            }
        }
    }
}