using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class ListWriterBuilder : WriterBuilderBase
    {
        public ListWriterBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(List<>)))
                throw new InvalidOperationException("Expected list but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            var emptyLabel = il.DefineLabel("empty");
            context.LoadObj(); // stack: [obj]
            il.Brfalse(emptyLabel); // if(obj == null) goto empty;
            context.LoadObj(); // stack: [obj]
            context.Il.Ldfld(Type.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.Count]
            il.Brtrue(notEmptyLabel); // if(obj.Count != 0) goto notEmpty;
            il.MarkLabel(emptyLabel);
            return true;
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.Array);
            var doneLabel = il.DefineLabel("done");
            var count = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.Count]
            il.Stloc(count); // count = obj.Count
            var items = il.DeclareLocal(elementType.MakeArrayType());
            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj._items]
            il.Stloc(items); // items = obj._items; stack: []
            context.LoadIndex(); // stack: [index]
            var start = context.LocalInt;
            il.Stloc(start); // start = index
            context.IncreaseIndexBy4(); // index = index + 4
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(count); // stack: [&result[index], count]
            il.Stind(typeof(int)); // *(int*)&result[index] = count; stack: []
            context.IncreaseIndexBy4(); // index = index + 4

            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [0]
            il.Stloc(i); // i = 0; stack: []
            var cycleStart = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStart);
            il.Ldloc(items); // stack: [items]
            il.Ldloc(i); // stack: [items, i]
            il.Ldelem(elementType);
            il.Ldc_I4(1); // stack: [obj[i], true]
            context.LoadResult(); // stack: [obj[i], true, result]
            context.LoadIndexByRef(); // stack: [obj[i], true, result, ref index]
            context.CallWriter(elementType); // writer(obj[i], true, result, ref index); stack: []
            il.Ldloc(count); // stack: [count]
            il.Ldloc(i); // stack: [count, i]
            il.Ldc_I4(1); // stack: [count, i, 1]
            il.Add(); // stack: [count, i + 1]
            il.Dup(); // stack: [count, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [count, i]
            il.Bgt(typeof(int), cycleStart); // if(count > i) goto cycleStart; stack: []

            context.LoadResult(); // stack: [result]
            il.Ldloc(start); // stack: [result, start]
            il.Add(); // stack: [result + start]
            context.LoadIndex(); // stack: [result + start, index]
            il.Ldloc(start); // stack: [result + start, index, start]
            il.Sub(); // stack: [result + start, index - start]
            il.Ldc_I4(4); // stack: [result + start, index - start, 4]
            il.Sub(); // stack: [result + start, index - start - 4]
            il.Stind(typeof(int)); // *(int*)(result + start) = index - start - 4

            il.MarkLabel(doneLabel);
        }

        private readonly Type elementType;
    }
}