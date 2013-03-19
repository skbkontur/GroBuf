using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class DictionaryWriterBuilder : WriterBuilderBase
    {
        public DictionaryWriterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                throw new InvalidOperationException("Dictionary expected but was '" + Type + "'");
            keyType = Type.GetGenericArguments()[0];
            valueType = Type.GetGenericArguments()[1];
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var emptyLabel = context.Il.DefineLabel("empty");
            context.LoadObj(); // stack: [obj]
            context.Il.Brfalse(emptyLabel); // if(obj == null) goto empty;
            context.LoadObj(); // stack: [obj]
            context.Il.Call(Type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(), Type); // stack: [obj.Length]
            context.Il.Brtrue(notEmptyLabel); // if(obj.Length != 0) goto notEmpty;
            context.Il.MarkLabel(emptyLabel);
            return true;
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(keyType);
            context.BuildConstants(valueType);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.Dictionary);
            var doneLabel = il.DefineLabel("done");
            context.LoadIndex(); // stack: [index]
            var start = context.LocalInt;
            il.Stloc(start); // start = index
            context.IncreaseIndexBy4(); // index = index + 4
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [obj]
            context.Il.Call(Type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(), Type); // stack: [obj.Count]
            il.Stind(typeof(int)); // *(int*)&result[index] = count; stack: []
            context.IncreaseIndexBy4(); // index = index + 4

            context.LoadObj(); // stack: [obj]
            var keyValueType = typeof(KeyValuePair<,>).MakeGenericType(Type.GetGenericArguments());
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(keyValueType);
            var enumerator = il.DeclareLocal(enumeratorType);
            il.Call(typeof(IEnumerable<>).MakeGenericType(keyValueType).GetMethod("GetEnumerator"), Type); // stack: [obj.GetEnumerator()]
            il.Dup(); // stack: [enumerator]
            il.Stloc(enumerator); // enumerator = obj.GetEnumerator(); stack: [size, enumerator]
            il.Call(typeof(IEnumerator).GetMethod("Reset"), typeof(IEnumerator)); // enumerator.Reset(); stack: [size]
            var endLabel = il.DefineLabel("end");
            il.Ldloc(enumerator);
            il.Call(typeof(IEnumerator).GetMethod("MoveNext"), typeof(IEnumerator));
            il.Brfalse(endLabel);
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            il.Ldloc(enumerator); // stack: [size, enumerator]
            il.Call(enumeratorType.GetProperty("Current").GetGetMethod(), enumeratorType); // stack: [size, enumerator.Current]
            var current = il.DeclareLocal(keyValueType);
            il.Stloc(current);
            il.Ldloca(current);
            il.Call(keyValueType.GetProperty("Key").GetGetMethod(), keyValueType);
            il.Ldc_I4(1);
            context.LoadResult(); // stack: [obj[i], true, result]
            context.LoadIndexByRef();
            context.CallWriter(keyType);
            il.Ldloca(current);
            il.Call(keyValueType.GetProperty("Value").GetGetMethod(), keyValueType);
            il.Ldc_I4(1);
            context.LoadResult(); // stack: [obj[i], true, result]
            context.LoadIndexByRef();
            context.CallWriter(valueType);
            il.Ldloc(enumerator);
            il.Call(typeof(IEnumerator).GetMethod("MoveNext"), typeof(IEnumerator));
            il.Brtrue(cycleStartLabel);
            il.MarkLabel(endLabel);

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

        private readonly Type keyType;
        private readonly Type valueType;
    }
}