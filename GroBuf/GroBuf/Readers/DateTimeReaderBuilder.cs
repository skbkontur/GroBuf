using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class DateTimeReaderBuilder : ReaderBuilderBase<DateTime>
    {
        protected override void ReadNotEmpty(ReaderMethodBuilderContext<DateTime> context)
        {
            context.AssertTypeCode(GroBufTypeCode.Int64); // Assert typeCode == TypeCode.Int64

            var il = context.Il;
            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            il.Emit(OpCodes.Call, context.Context.GetReader(typeof(long))); // stack: [reader(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Ldc_I4, (int)DateTimeKind.Utc);
            il.Emit(OpCodes.Newobj, Type.GetConstructor(new[] {typeof(long), typeof(DateTimeKind)})); // stack: new DateTime(reader(pinnedData, ref index), DateTimeKind.Utc)
        }
    }
}