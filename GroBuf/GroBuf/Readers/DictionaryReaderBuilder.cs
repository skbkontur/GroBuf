using System;
using System.Collections.Generic;

namespace GroBuf.Readers
{
    internal class DictionaryReaderBuilder : ReaderBuilderBase
    {
        public DictionaryReaderBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                throw new InvalidOperationException("Dictionary expected but was '" + Type + "'");
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCode.Dictionary);

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
            il.Ldloc(length); // stack: [ref result, length]
            il.Newobj(Type.GetConstructor(new[] {typeof(int)})); // stack: [ref result, new Dictionary(length)]
            il.Stind(typeof(object)); // result = new Dictionary(length); stack: []

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
            var key = il.DeclareLocal(Type.GetGenericArguments()[0]);
            var value = il.DeclareLocal(Type.GetGenericArguments()[1]);
            il.Ldloca(key); // stack: [pinnedData, ref index, dataLength, ref key]
            il.Call(context.Context.GetReader(Type.GetGenericArguments()[0])); // reader(pinnedData, ref index, dataLength, ref key); stack: []

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            il.Ldloca(value); // stack: [pinnedData, ref index, dataLength, ref value]
            il.Call(context.Context.GetReader(Type.GetGenericArguments()[1])); // reader(pinnedData, ref index, dataLength, ref value); stack: []

            context.LoadResult(Type);
            il.Ldloc(key);
            il.Ldloc(value);
            il.Call(Type.GetMethod("Add"), Type);

            il.Ldloc(i); // stack: [i]
            il.Ldc_I4(1); // stack: [i, 1]
            il.Add(); // stack: [i + 1]
            il.Dup(); // stack: [i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [i]
            il.Ldloc(length); // stack: [i, length]
            il.Blt(typeof(uint), cycleStartLabel); // if(i < length) goto cycleStart
            il.MarkLabel(doneLabel); // stack: []
        }
    }
}