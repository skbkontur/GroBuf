using System;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class DateTimeWriterBuilder : WriterBuilderBase<DateTime>
    {
        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.DateTime);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.Emit(OpCodes.Call, Type.GetProperty("Ticks").GetGetMethod()); // stack: [&result[index], obj.Ticks]
            il.Emit(OpCodes.Stind_I8); // (long)&result[index] = ticks
            context.IncreaseIndexBy8(); // index = index + 8
        }
    }
}