using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal class ArrayWriterBuilder<T> : WriterBuilderWithOneParam<T, Delegate>
    {
        public ArrayWriterBuilder(IWriterCollection writerCollection)
            : base(writerCollection)
        {
            if(!Type.IsArray) throw new InvalidOperationException("An array expected but was " + Type);
            if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
        }

        protected override bool CheckEmpty(WriterBuilderContext context, Label notEmptyLabel)
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

        protected override Delegate WriteNotEmpty(WriterBuilderContext context)
        {
            var il = context.Il;
            var allDoneLabel = il.DefineLabel();
            var length = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [obj]
            il.Emit(OpCodes.Ldlen); // stack: [obj.Length]
            il.Emit(OpCodes.Stloc, length); // length = obj.Length
            il.Emit(OpCodes.Ldc_I4, 9); // stack: [9]
            context.EnsureSize();
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.Array); // stack: [&result[index], TypeCode.Array]
            il.Emit(OpCodes.Stind_I1); // result[index] = TypeCode.Array
            context.IncreaseIndexBy1(); // index = index + 1
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
            context.LoadAdditionalParam(0); // stack: [writer]
            context.LoadObj(); // stack: [writer, obj]
            il.Emit(OpCodes.Ldloc, i); // stack: [writer, obj, i]
            var elementType = Type.GetElementType();
            LoadArrayElement(elementType, il); // stack: [writer, obj[i]]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [writer, obj[i], true]
            context.LoadResultByRef(); // stack: [writer, obj[i], true, ref result]
            context.LoadIndexByRef(); // stack: [writer, obj[i], true, ref result, ref index]
            context.LoadPinnedResultByRef(); // stack: [writer, obj[i], true, ref result, ref index, ref pinnedResult]
            var writer = GetWriter(elementType);
            il.Emit(OpCodes.Call, writer.GetType().GetMethod("Invoke")); // writer(obj[i], true, ref result, ref index, ref pinnedResult); stack: []
            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            il.Emit(OpCodes.Ldloc, i); // stack: [length, i]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [length, i, 1]
            il.Emit(OpCodes.Add); // stack: [length, i + 1]
            il.Emit(OpCodes.Dup); // stack: [length, i + 1, i + 1]
            il.Emit(OpCodes.Stloc, i); // i = i + 1; stack: [length, i]
            il.Emit(OpCodes.Bgt, cycleStart); // if(length > i) goto cycleStart; stack: []

            context.LoadPinnedResult(); // stack: [pinnedResult]
            il.Emit(OpCodes.Ldloc, start); // stack: [pinnedResult, start]
            il.Emit(OpCodes.Add); // stack: [pinnedResult + start]
            context.LoadIndex(); // stack: [pinnedResult + start, index]
            il.Emit(OpCodes.Ldloc, start); // stack: [pinnedResult + start, index, start]
            il.Emit(OpCodes.Sub); // stack: [pinnedResult + start, index - start]
            il.Emit(OpCodes.Ldc_I4_4); // stack: [pinnedResult + start, index - start, 4]
            il.Emit(OpCodes.Sub); // stack: [pinnedResult + start, index - start - 4]
            il.Emit(OpCodes.Stind_I4); // *(int*)(pinnedResult + start) = index - start - 4

            il.MarkLabel(allDoneLabel);
            return writer;
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
                    throw new NotSupportedException();
                }
            }
        }
    }
}