using System;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class ArrayWriterBuilder<T> : WriterBuilderBase<T>
    {
        public ArrayWriterBuilder()
        {
            if (Type != typeof(Array))
            {
                if(!Type.IsArray) throw new InvalidOperationException("An array expected but was " + Type);
                if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
            }
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

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            var allDoneLabel = il.DefineLabel();
            var length = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [obj]
            il.Emit(OpCodes.Ldlen); // stack: [obj.Length]
            il.Emit(OpCodes.Stloc, length); // length = obj.Length
            context.WriteTypeCode(GroBufTypeCode.Array);
            context.LoadIndex(); // stack: [index]
            var start = context.LocalInt;
            il.Emit(OpCodes.Stloc, start); // start = index
            context.IncreaseIndexBy4(); // index = index + 4
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Emit(OpCodes.Ldloc, length); // stack: [&result[index], length]
            il.Emit(OpCodes.Stind_I4); // *(int*)&result[index] = length; stack: []
            context.IncreaseIndexBy4(); // index = index + 4

            var i = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Stloc, i); // i = 0; stack: []
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);
            context.LoadObj(); // stack: [obj]
            il.Emit(OpCodes.Ldloc, i); // stack: [obj, i]
            var elementType = Type.GetElementType() ?? typeof(object);
            LoadArrayElement(elementType, il); // stack: [obj[i]]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [obj[i], true]
            context.LoadResult(); // stack: [obj[i], true, result]
            context.LoadIndexByRef(); // stack: [obj[i], true, result, ref index]
            il.Emit(OpCodes.Call, context.Context.GetWriter(elementType)); // writer(obj[i], true, result, ref index); stack: []
            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            il.Emit(OpCodes.Ldloc, i); // stack: [length, i]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [length, i, 1]
            il.Emit(OpCodes.Add); // stack: [length, i + 1]
            il.Emit(OpCodes.Dup); // stack: [length, i + 1, i + 1]
            il.Emit(OpCodes.Stloc, i); // i = i + 1; stack: [length, i]
            il.Emit(OpCodes.Bgt, cycleStart); // if(length > i) goto cycleStart; stack: []

            context.LoadResult(); // stack: [result]
            il.Emit(OpCodes.Ldloc, start); // stack: [result, start]
            il.Emit(OpCodes.Add); // stack: [result + start]
            context.LoadIndex(); // stack: [result + start, index]
            il.Emit(OpCodes.Ldloc, start); // stack: [result + start, index, start]
            il.Emit(OpCodes.Sub); // stack: [result + start, index - start]
            il.Emit(OpCodes.Ldc_I4_4); // stack: [result + start, index - start, 4]
            il.Emit(OpCodes.Sub); // stack: [result + start, index - start - 4]
            il.Emit(OpCodes.Stind_I4); // *(int*)(result + start) = index - start - 4

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
                    throw new NotSupportedException("Type " + elementType.Name + " is not supported");
                }
            }
        }
    }
}