using System;
using System.Reflection;

namespace GroBuf.Writers
{
    internal class TimeSpanWriterBuilder : WriterBuilderBase
    {
        public TimeSpanWriterBuilder()
            : base(typeof(TimeSpan))
        {
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(typeof(long));
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;

            context.LoadObjByRef(); // stack: [obj]
            il.Ldfld(Type.GetField("_ticks", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj._ticks]
            context.LoadWriteEmpty(); // stack: [obj._ticks, writeEmpty]
            context.LoadResult(); // stack: [obj._ticks, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj._ticks, writeEmpty, result, ref index]
            context.LoadContext(); // stack: [obj._ticks, writeEmpty, result, ref index, context]
            context.CallWriter(typeof(long)); // writer(obj._ticks, writeEmpty, result, ref index, context)
        }

        protected override bool IsReference { get { return false; } }
    }
}