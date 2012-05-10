using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class ArrayReaderBuilder<T> : ReaderBuilderBase<T>
    {
        public ArrayReaderBuilder()
        {
            if(Type != typeof(Array))
            {
                if(!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
                if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
                elementType = Type.GetElementType();
            }
            else elementType = typeof(object);
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
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

            var createArrayLabel = il.DefineLabel();
            context.LoadResult(); // stack: [result]
            il.Emit(OpCodes.Brfalse, createArrayLabel); // if(result == null) goto createArray;
            context.LoadResult(); // stack: [result]
            il.Emit(OpCodes.Ldlen); // stack: [result.Length]
            il.Emit(OpCodes.Ldloc, length); // stack: [result.Length, length]
            var arrayCreatedLabel = il.DefineLabel();
            il.Emit(OpCodes.Bge, arrayCreatedLabel); // if(result.Length >= length) goto arrayCreated;

            context.LoadResultByRef(); // stack: [ref result]
            il.Emit(OpCodes.Ldloc, length); // stack: [ref result, length]
            il.Emit(OpCodes.Call, resizeMethod.MakeGenericMethod(elementType)); // Array.Resize(ref result, length)
            il.Emit(OpCodes.Br, arrayCreatedLabel); // goto arrayCreated

            il.MarkLabel(createArrayLabel);
            context.LoadResultByRef(); // stack: [ref result]
            il.Emit(OpCodes.Ldloc, length); // stack: [ref result, length]
            il.Emit(OpCodes.Newarr, elementType); // stack: [ref result, new type[length]]
            il.Emit(OpCodes.Stind_Ref); // result = new type[length]; stack: []

            il.MarkLabel(arrayCreatedLabel);
            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, allDoneLabel); // if(length == 0) goto allDone; stack: []
            var i = il.DeclareLocal(typeof(uint));
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Stloc, i); // i = 0; stack: []
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            context.LoadResult(); // stack: [pinnedData, ref index, dataLength, result]
            il.Emit(OpCodes.Ldloc, i); // stack: [pinnedData, ref index, dataLength, result, i]

            il.Emit(OpCodes.Ldelema, elementType); // stack: [pinnedData, ref index, dataLength, ref result[i]]

            il.Emit(OpCodes.Call, context.Context.GetReader(elementType)); // reader(pinnedData, ref index, dataLength, ref result[i]); stack: []
            il.Emit(OpCodes.Ldloc, i); // stack: [i]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [i, 1]
            il.Emit(OpCodes.Add); // stack: [i + 1]
            il.Emit(OpCodes.Dup); // stack: [i + 1, i + 1]
            il.Emit(OpCodes.Stloc, i); // i = i + 1; stack: [i]
            il.Emit(OpCodes.Ldloc, length); // stack: [i, length]
            il.Emit(OpCodes.Blt_Un, cycleStart); // if(i < length) goto cycleStart
            il.MarkLabel(allDoneLabel); // stack: []
        }

        private static readonly MethodInfo resizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 0))).Body).Method.GetGenericMethodDefinition();
        private readonly Type elementType;
    }
}