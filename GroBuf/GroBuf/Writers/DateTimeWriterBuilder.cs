using System;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class DateTimeWriterBuilder : WriterBuilderBase<DateTime>
    {
        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.LoadObjByRef(); // stack: [&obj]
            il.Emit(OpCodes.Call, Type.GetProperty("Ticks").GetGetMethod()); // stack: [obj.Ticks]
            context.LoadWriteEmpty(); // stack: [obj.Value, writeEmpty]
            context.LoadResultByRef(); // stack: [obj.Value, writeEmpty, ref result]
            context.LoadIndexByRef(); // stack: [obj.Value, writeEmpty, ref result, ref index]
            context.LoadPinnedResultByRef(); // stack: [obj.Value, writeEmpty, ref result, ref index, ref pinnedResult]
            il.Emit(OpCodes.Call, context.Context.GetWriter(typeof(long))); // writer(obj.Ticks, writeEmpty, ref result, ref index, ref pinnedResult)
        }
    }
}