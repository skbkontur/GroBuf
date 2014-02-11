using System;

namespace GroBuf.Readers
{
    internal class TimeSpanReaderBuilder : ReaderBuilderBase
    {
        public TimeSpanReaderBuilder()
            : base(typeof(TimeSpan))
        {
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(typeof(long));
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;
            context.LoadResultByRef(); // stack: [ref result]

            context.LoadData(); // stack: [ref result, data]
            context.LoadIndexByRef(); // stack: [ref result, data, ref index]
            context.LoadDataLength(); // stack: [ref result, data, ref index, dataLength]
            var ticks = il.DeclareLocal(typeof(long));
            il.Ldloca(ticks); // stack: [ref result, data, ref index, dataLength, ref value]
            context.CallReader(typeof(long)); // reader(pinnedData, ref index, dataLength, ref value); stack: [ref result]
            il.Ldloc(ticks); // stack: [ref result, value]
            var constructor = Type.GetConstructor(new[] {typeof(long)});
            if(constructor == null)
                throw new MissingConstructorException(Type, typeof(long));
            il.Newobj(constructor); // stack: [ref result, new TimeSpan(ticks)]
            il.Stobj(Type); // result = new TimeSpan(ticks)
        }
    }
}