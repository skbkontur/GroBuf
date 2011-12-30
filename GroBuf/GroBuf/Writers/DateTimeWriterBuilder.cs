using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal class DateTimeWriterBuilder : WriterBuilderWithOneParam<DateTime, Delegate>
    {
        public DateTimeWriterBuilder(IWriterCollection writerCollection)
            : base(writerCollection)
        {
        }

        protected override Delegate WriteNotEmpty(WriterBuilderContext context)
        {
            var il = context.Il;
            context.LoadAdditionalParam(0); // stack: [writer]
            context.LoadObjByRef(); // stack: [writer, &obj]
            il.Emit(OpCodes.Call, Type.GetProperty("Ticks").GetGetMethod()); // stack: [writer, obj.Ticks]
            context.LoadWriteEmpty(); // stack: [writer, obj.Value, writeEmpty]
            context.LoadResultByRef(); // stack: [writer, obj.Value, writeEmpty, ref result]
            context.LoadIndexByRef(); // stack: [writer, obj.Value, writeEmpty, ref result, ref index]
            context.LoadPinnedResultByRef(); // stack: [writer, obj.Value, writeEmpty, ref result, ref index, ref pinnedResult]
            var writer = GetWriter(typeof(long));
            il.Emit(OpCodes.Call, writer.GetType().GetMethod("Invoke")); // writer(obj.Ticks, writeEmpty, ref result, ref index, ref pinnedResult)
            return writer;
        }
    }
}