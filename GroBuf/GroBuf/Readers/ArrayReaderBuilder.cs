using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GroBuf.Readers
{
    internal class ArrayReaderBuilder : ReaderBuilderBase
    {
        public ArrayReaderBuilder(Type type)
            : base(type)
        {
            if(Type != typeof(Array))
            {
                if(!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
                if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
                elementType = Type.GetElementType();
            }
            else elementType = typeof(object);
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
                il.Ldlen(); // stack: [result.Length]
                il.Ldloc(length); // stack: [result.Length, length]
                var arrayCreatedLabel = il.DefineLabel("arrayCreated");
                il.Bge(typeof(int), arrayCreatedLabel); // if(result.Length >= length) goto arrayCreated;

                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(length); // stack: [ref result, length]
                il.Call(resizeMethod.MakeGenericMethod(elementType)); // Array.Resize(ref result, length)
                il.Br(arrayCreatedLabel); // goto arrayCreated

                il.MarkLabel(createArrayLabel);
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(length); // stack: [ref result, length]
                il.Newarr(elementType); // stack: [ref result, new type[length]]
                il.Stind(typeof(object)); // result = new type[length]; stack: []

                il.MarkLabel(arrayCreatedLabel);
            }
            else
            {
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(length); // stack: [ref result, length]
                il.Newarr(elementType); // stack: [ref result, new type[length]]
                il.Stind(typeof(object)); // result = new type[length]; stack: []
            }
            il.Ldloc(length); // stack: [length]
            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(length == 0) goto allDone; stack: []
            var i = il.DeclareLocal(typeof(uint));
            il.Ldc_I4(0); // stack: [0]
            il.Stloc(i); // i = 0; stack: []
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            context.LoadResult(Type); // stack: [pinnedData, ref index, dataLength, result]
            il.Ldloc(i); // stack: [pinnedData, ref index, dataLength, result, i]

            il.Ldelema(elementType); // stack: [pinnedData, ref index, dataLength, ref result[i]]

            context.CallReader(elementType); // reader(pinnedData, ref index, dataLength, ref result[i]); stack: []
            il.Ldloc(i); // stack: [i]
            il.Ldc_I4(1); // stack: [i, 1]
            il.Add(); // stack: [i + 1]
            il.Dup(); // stack: [i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [i]
            il.Ldloc(length); // stack: [i, length]
            il.Blt(typeof(uint), cycleStartLabel); // if(i < length) goto cycleStart
            il.MarkLabel(doneLabel); // stack: []
        }

        private static readonly MethodInfo resizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 0))).Body).Method.GetGenericMethodDefinition();
        private readonly Type elementType;
    }
}