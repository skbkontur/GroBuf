using System.Collections;

namespace GroBuf.Readers
{
    internal class HashtableReaderBuilder : ReaderBuilderBase
    {
        public HashtableReaderBuilder()
            : base(typeof(Hashtable))
        {
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(typeof(object));
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
            il.Newobj(Type.GetConstructor(new[] {typeof(int)})); // stack: [ref result, new Hashtable(length)]
            il.Stind(Type); // result = new Hashtable(length); stack: []

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
            var key = il.DeclareLocal(typeof(object));
            var value = il.DeclareLocal(typeof(object));
            il.Ldloca(key); // stack: [pinnedData, ref index, ref key]
            context.LoadContext(); // stack: [pinnedData, ref index, ref key, context]
            context.CallReader(typeof(object)); // reader(pinnedData, ref index, ref key, context); stack: []

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            il.Ldloca(value); // stack: [pinnedData, ref index, ref value]
            context.LoadContext(); // stack: [pinnedData, ref index, ref value, context]
            context.CallReader(typeof(object)); // reader(pinnedData, ref index, ref value, context); stack: []

            context.LoadResult(Type);
            il.Ldloc(key);
            il.Ldloc(value);
            il.Call(Type.GetMethod("Add"));

            il.Ldnull();
            il.Stloc(key);
            il.Ldnull();
            il.Stloc(value);

            il.Ldloc(i); // stack: [i]
            il.Ldc_I4(1); // stack: [i, 1]
            il.Add(); // stack: [i + 1]
            il.Dup(); // stack: [i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [i]
            il.Ldloc(length); // stack: [i, length]
            il.Blt(cycleStartLabel, true); // if(i < length) goto cycleStart
            il.MarkLabel(doneLabel); // stack: []
        }

        protected override bool IsReference => true;
    }
}