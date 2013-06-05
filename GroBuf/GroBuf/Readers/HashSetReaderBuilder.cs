using System;
using System.Collections.Generic;
using System.Reflection;

namespace GroBuf.Readers
{
    internal class HashSetReaderBuilder : ReaderBuilderBase
    {
        public HashSetReaderBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(HashSet<>)))
                throw new InvalidOperationException("HashSet expected but was '" + Type + "'");
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

            context.LoadResultByRef(); // stack: [ref result]
            il.Newobj(Type.GetConstructor(Type.EmptyTypes)); // stack: [ref result, new HashSet() = hashSet]
            il.Dup(); // stack: [ref result, hashSet, hashSet]
            il.Ldloc(length); // stack: [ref result, hashSet, hashSet, length]
            il.Call(Type.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic), Type); // hashSet.Initialize(length); stack: [ref result, hashSet]
            il.Stind(typeof(object)); // result = hashSet; stack: []

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
            var value = il.DeclareLocal(elementType);
            il.Ldloca(value); // stack: [pinnedData, ref index, dataLength, ref value]
            context.CallReader(elementType); // reader(pinnedData, ref index, dataLength, ref value); stack: []

            context.LoadResult(Type); // stack: [result]
            il.Ldloc(value); // stack: [result, value]
            il.Call(Type.GetMethod("Add"), Type); // stack: [result.Add(value)]
            il.Pop(); // stack: []

            il.Ldloc(i); // stack: [i]
            il.Ldc_I4(1); // stack: [i, 1]
            il.Add(); // stack: [i + 1]
            il.Dup(); // stack: [i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [i]
            il.Ldloc(length); // stack: [i, length]
            il.Blt(typeof(uint), cycleStartLabel); // if(i < length) goto cycleStart
            il.MarkLabel(doneLabel); // stack: []
        }

        private readonly Type elementType;
    }
}