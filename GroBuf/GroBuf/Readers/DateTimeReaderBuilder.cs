using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class DateTimeReaderBuilder : ReaderBuilderWithOneParam<DateTime, Delegate>
    {
        public DateTimeReaderBuilder(IReaderCollection readerCollection)
            : base(readerCollection)
        {
        }

        protected override Delegate ReadNotEmpty(ReaderBuilderContext<DateTime> context)
        {
            context.AssertTypeCode(GroBufTypeCode.Int64); // Assert typeCode == TypeCode.Int64

            var il = context.Il;
            context.LoadAdditionalParam(0); // stack: [reader]
            context.LoadData(); // stack: [reader, pinnedData]
            context.LoadIndexByRef(); // stack: [reader, pinnedData, ref index]
            context.LoadDataLength(); // stack: [reader, pinnedData, ref index, dataLength]
            var reader = GetReader(typeof(long));
            il.Emit(OpCodes.Call, reader.GetType().GetMethod("Invoke")); // stack: [reader(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Ldc_I4, (int)DateTimeKind.Utc);
            il.Emit(OpCodes.Newobj, Type.GetConstructor(new[] {typeof(long), typeof(DateTimeKind)})); // stack: new DateTime(reader(pinnedData, ref index), DateTimeKind.Utc)
            return reader;
        }
    }
}