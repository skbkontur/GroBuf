using System;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class DateTimeReaderBuilder : ReaderBuilderBase<DateTime>
    {
        protected override void ReadNotEmpty(ReaderMethodBuilderContext<DateTime> context)
        {
            context.AssertTypeCode(GroBufTypeCode.Int64); // Assert typeCode == TypeCode.Int64

            var il = context.Il;
            var ticks = il.DeclareLocal(typeof(long));
            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            il.Emit(OpCodes.Ldloca, ticks); // stack: [pinnedData, ref index, dataLength, ref ticks]
            il.Emit(OpCodes.Call, context.Context.GetReader(typeof(long))); // reader(pinnedData, ref index, dataLength, ref ticks); stack: []
            context.LoadResultByRef(); // stack: [ref result]
            il.Emit(OpCodes.Ldloc, ticks); // stack: [ref result, ticks]
            il.Emit(OpCodes.Ldc_I4, (int)DateTimeKind.Utc); // stack: [ref result, ticks, DateTimeKind.Utc]
            var constructor = Type.GetConstructor(new[] {typeof(long), typeof(DateTimeKind)});
            if(constructor == null)
                throw new MissingConstructorException(Type, typeof(long), typeof(DateTimeKind));
            il.Emit(OpCodes.Newobj, constructor); // stack: [ref result, new DateTime(ticks, DateTimeKind.Utc)]
            il.Emit(OpCodes.Stobj, Type);
        }
    }
}