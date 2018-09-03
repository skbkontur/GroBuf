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
            var ticks = il.DeclareLocal(typeof(long));
            il.Ldloca(ticks); // stack: [ref result, data, ref index, ref value]
            context.LoadContext(); // stack: [ref result, data, ref index, ref value, context]
            context.CallReader(typeof(long)); // reader(pinnedData, ref index, ref value, context); stack: [ref result]
            il.Ldloc(ticks); // stack: [ref result, value]
            var constructor = Type.GetConstructor(new[] {typeof(long)});
            if (constructor == null)
                throw new MissingConstructorException(Type, typeof(long));
            il.Newobj(constructor); // stack: [ref result, new TimeSpan(ticks)]
            il.Stobj(Type); // result = new TimeSpan(ticks)
        }

        protected override bool IsReference { get { return false; } }
    }
}