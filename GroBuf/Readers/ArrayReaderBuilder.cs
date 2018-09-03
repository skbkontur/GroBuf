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
            if (!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
            if (Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
            elementType = Type.GetElementType();
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;

            il.Ldloc(context.TypeCode); // stack: [type code]
            il.Ldc_I4((int)GroBufTypeCode.Array); // stack: [type code, GroBufTypeCode.Array]
            var tryReadArrayElementLabel = il.DefineLabel("tryReadArrayElement");
            il.Bne_Un(tryReadArrayElementLabel); // if(type code != GroBufTypeCode.Array) goto tryReadArrayElement; stack: []

            context.IncreaseIndexBy1();
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
                il.Bge(arrayCreatedLabel, false); // if(result.Length >= length) goto arrayCreated;

                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(length); // stack: [ref result, length]
                il.Call(resizeMethod.MakeGenericMethod(elementType)); // Array.Resize(ref result, length)
                il.Br(arrayCreatedLabel); // goto arrayCreated

                il.MarkLabel(createArrayLabel);
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(length); // stack: [ref result, length]
                il.Newarr(elementType); // stack: [ref result, new type[length]]
                il.Stind(Type); // result = new type[length]; stack: []

                il.MarkLabel(arrayCreatedLabel);
            }
            else
            {
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(length); // stack: [ref result, length]
                il.Newarr(elementType); // stack: [ref result, new type[length]]
                il.Stind(Type); // result = new type[length]; stack: []
            }

            context.StoreObject(Type);

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
            context.LoadResult(Type); // stack: [pinnedData, ref index, result]
            il.Ldloc(i); // stack: [pinnedData, ref index, result, i]

            il.Ldelema(elementType); // stack: [pinnedData, ref index, ref result[i]]
            context.LoadContext(); // stack: [pinnedData, ref index, ref result[i], context]

            context.CallReader(elementType); // reader(pinnedData, ref index, ref result[i], context); stack: []
            il.Ldloc(i); // stack: [i]
            il.Ldc_I4(1); // stack: [i, 1]
            il.Add(); // stack: [i + 1]
            il.Dup(); // stack: [i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [i]
            il.Ldloc(length); // stack: [i, length]
            il.Blt(cycleStartLabel, true); // if(i < length) goto cycleStart
            il.Br(doneLabel);

            il.MarkLabel(tryReadArrayElementLabel);

            if (context.Context.GroBufReader.Options.HasFlag(GroBufOptions.MergeOnRead))
            {
                var createArrayLabel = il.DefineLabel("createArray");
                context.LoadResult(Type); // stack: [result]
                il.Brfalse(createArrayLabel); // if(result == null) goto createArray;
                context.LoadResult(Type); // stack: [result]
                il.Ldlen(); // stack: [result.Length]
                il.Ldc_I4(1); // stack: [result.Length, 1]
                var arrayCreatedLabel = il.DefineLabel("arrayCreated");
                il.Bge(arrayCreatedLabel, false); // if(result.Length >= 1) goto arrayCreated;

                context.LoadResultByRef(); // stack: [ref result]
                il.Ldc_I4(1); // stack: [ref result, 1]
                il.Call(resizeMethod.MakeGenericMethod(elementType)); // Array.Resize(ref result, length)
                il.Br(arrayCreatedLabel); // goto arrayCreated

                il.MarkLabel(createArrayLabel);
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldc_I4(1); // stack: [ref result, 1]
                il.Newarr(elementType); // stack: [ref result, new type[1]]
                il.Stind(Type); // result = new type[1]; stack: []

                il.MarkLabel(arrayCreatedLabel);
            }
            else
            {
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldc_I4(1); // stack: [ref result, 1]
                il.Newarr(elementType); // stack: [ref result, new type[1]]
                il.Stind(Type); // result = new type[1]; stack: []
            }

            context.StoreObject(Type);

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadResult(Type); // stack: [pinnedData, ref index, result]
            il.Ldc_I4(0); // stack: [pinnedData, ref index, result, 0]

            il.Ldelema(elementType); // stack: [pinnedData, ref index, ref result[0]]
            context.LoadContext(); // stack: [pinnedData, ref index, ref result[0], context]

            context.CallReader(elementType); // reader(pinnedData, ref index, ref result[0], context); stack: []

            il.MarkLabel(doneLabel); // stack: []
        }

        protected override bool IsReference => true;

        private static readonly MethodInfo resizeMethod = ((MethodCallExpression)((Expression<Action<int[]>>)(arr => Array.Resize(ref arr, 0))).Body).Method.GetGenericMethodDefinition();
        private readonly Type elementType;
    }
}