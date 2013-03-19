using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    // todo переписать
    internal class DictionarySizeCounterBuilder : SizeCounterBuilderBase
    {
        public DictionarySizeCounterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                throw new InvalidOperationException("Dictionary expected but was '" + Type + "'");
            keyType = Type.GetGenericArguments()[0];
            valueType = Type.GetGenericArguments()[1];
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(keyType);
            context.BuildConstants(valueType);
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
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

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(9); // stack: [9 = size] 9 = type code + data length + dictionary count
            context.LoadObj(); // stack: [size, obj]
            var keyValueType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(keyValueType);
            var enumerator = il.DeclareLocal(enumeratorType);
            il.Call(typeof(IEnumerable<>).MakeGenericType(keyValueType).GetMethod("GetEnumerator"), Type); // stack: [size, obj.GetEnumerator()]
            il.Dup(); // stack: [size, enumerator]
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
            context.CallSizeCounter(keyType);
            il.Add();
            il.Ldloca(current);
            il.Call(keyValueType.GetProperty("Value").GetGetMethod(), keyValueType);
            il.Ldc_I4(1);
            context.CallSizeCounter(valueType);
            il.Add();
            il.Ldloc(enumerator);
            il.Call(typeof(IEnumerator).GetMethod("MoveNext"), typeof(IEnumerator));
            il.Brtrue(cycleStartLabel);
            il.MarkLabel(endLabel);
        }

        private readonly Type keyType;
        private readonly Type valueType;
    }
}