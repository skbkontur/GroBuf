using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GroBuf.Readers
{
    internal class ListReaderBuilder : ReaderBuilderBase
    {
        public ListReaderBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(List<>)))
                throw new InvalidOperationException("Expected list but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCode.Array);

            var il = context.Il;
            var length = context.Length;

            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [data length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [data length]

            context.AssertLength();
            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [array length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [array length]
            il.Stloc(length); // length = array length; stack: []

            if (context.Context.GroBufReader.Options.HasFlag(GroBufOptions.MergeOnRead))
            {
                var createArrayLabel = il.DefineLabel("createArray");
                context.LoadResult(Type); // stack: [result]
                il.Brfalse(createArrayLabel); // if(result == null) goto createArray;
                context.LoadResult(Type); // stack: [result]
                il.Ldfld(Type.GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [result._items]
                il.Ldlen(); // stack: [result._items.Length]
                il.Ldloc(length); // stack: [result._items.Length, length]

                var arrayCreatedLabel = il.DefineLabel("arrayCreated");
                il.Bge(typeof(int), arrayCreatedLabel); // if(result._items.Length >= length) goto arrayCreated;

                context.LoadResult(Type); // stack: [result]
                il.Ldflda(Type.GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [ref result._items]
                il.Ldloc(length); // stack: [ref result, length]
                il.Call(resizeMethod.MakeGenericMethod(elementType)); // Array.Resize(ref result._items, length)
                il.Br(arrayCreatedLabel); // goto arrayCreated

                il.MarkLabel(createArrayLabel);
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(length); // stack: [ref result, length]
                il.Newobj(Type.GetConstructor(new[] {typeof(int)})); // stack: [ref result, new List(length)]
                il.Stind(Type); // result = new List(length); stack: []

                il.MarkLabel(arrayCreatedLabel);
            }
            else
            {
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(length); // stack: [ref result, length]
                il.Newobj(Type.GetConstructor(new[] { typeof(int) })); // stack: [ref result, new List(length)]
                il.Stind(Type); // result = new List(length); stack: []
            }

            il.Ldloc(length); // stack: [length]
            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(length == 0) goto allDone; stack: []
            context.LoadResult(Type); // stack: [result]
            il.Ldfld(Type.GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [result._items]
            var items = il.DeclareLocal(elementType.MakeArrayType());
            il.Stloc(items); // items = result._items; stack: []
            var i = il.DeclareLocal(typeof(uint));
            il.Ldc_I4(0); // stack: [0]
            il.Stloc(i); // i = 0; stack: []
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            il.Ldloc(items); // stack: [pinnedData, ref index, dataLength, items]
            il.Ldloc(i); // stack: [pinnedData, ref index, dataLength, items, i]

            il.Ldelema(elementType); // stack: [pinnedData, ref index, dataLength, ref items[i]]

            context.CallReader(elementType); // reader(pinnedData, ref index, dataLength, ref result[i]); stack: []
            il.Ldloc(i); // stack: [i]
            il.Ldc_I4(1); // stack: [i, 1]
            il.Add(); // stack: [i + 1]
            il.Dup(); // stack: [i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [i]
            il.Ldloc(length); // stack: [i, length]
            il.Blt(typeof(uint), cycleStartLabel); // if(i < length) goto cycleStart

            if (context.Context.GroBufReader.Options.HasFlag(GroBufOptions.MergeOnRead))
            {
                context.LoadResult(Type); // stack: [result]
                il.Ldfld(Type.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [result.Count]
                il.Ldloc(length); // stack: [result.Count, length]
                il.Bge(typeof(int), doneLabel); // if(result.Count >= length) goto done; stack: []
            }
            context.LoadResult(Type); // stack: [result]
            il.Ldloc(length); // stack: [result.Count, length]
            il.Stfld(Type.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic)); // result._size = length; stack: []

            il.MarkLabel(doneLabel); // stack: []
        }

        private static readonly MethodInfo resizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 0))).Body).Method.GetGenericMethodDefinition();
        private readonly Type elementType;
    }
}