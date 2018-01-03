using System;

namespace GroBuf.Readers
{
    internal class TupleReaderBuilder : ReaderBuilderBase
    {
        public TupleReaderBuilder(Type type)
            : base(type)
        {
            if(!Type.IsTuple())
                throw new InvalidOperationException("A tuple expected but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            foreach(var argumentType in Type.GetGenericArguments())
                context.BuildConstants(argumentType);
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;

            context.AssertTypeCode(GroBufTypeCode.Tuple);

            context.IncreaseIndexBy1();
            var length = context.Length;

            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [data length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [array length]
            il.Stloc(length); // length = array length; stack: []

            context.LoadResultByRef(); // stack: [ref result]
            var genericArguments = Type.GetGenericArguments();
            // stack: [ref result, args]
            for(int i = 0; i < genericArguments.Length; ++i)
            {
                context.LoadData(); // stack: [ref result, args, data]
                context.LoadIndexByRef(); // stack: [ref result, args, data, ref index]
                var arg = il.DeclareLocal(genericArguments[i]);
                il.Ldloca(arg); // stack: [ref result, args, data, ref index, ref arg]
                context.LoadContext(); // stack: [ref result, args, data, ref index, ref arg, context]
                context.CallReader(genericArguments[i]); // reader(pinnedData, ref index, ref arg, context); stack: [ref result, args]
                il.Ldloc(arg); // stack: [ref result, {args, arg} => args]
            }
            il.Newobj(Type.GetConstructor(genericArguments)); // stack: [ref result, new Tuple(args)]
            il.Stind(Type); // result = new Tuple(args); stack: []

            context.StoreObject(Type);
        }

        protected override bool IsReference { get { return true; } }
    }
}