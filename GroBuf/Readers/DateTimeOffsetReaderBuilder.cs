using System;
using System.Reflection;

namespace GroBuf.Readers
{
    internal class DateTimeOffsetReaderBuilder : ReaderBuilderBase
    {
        public DateTimeOffsetReaderBuilder()
            : base(typeof(DateTimeOffset))
        {
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(typeof(DateTime));
            context.BuildConstants(typeof(short));
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCode.DateTimeOffset); // Assert typeCode == TypeCode.DateTimeOffset
            var il = context.Il;
            context.LoadResultByRef(); // stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Initobj(Type); // result = default(DateTimeOffset); stack: [ref result]

            context.LoadData(); // stack: [ref result, data]
            context.LoadIndexByRef(); // stack: [ref result, data, ref index]
            var dateTime = il.DeclareLocal(typeof(DateTime));
            il.Ldloca(dateTime); // stack: [ref result, data, ref index, ref dateTime]
            context.LoadContext(); // stack: [ref result, data, ref index, ref dateTime, context]
            context.CallReader(typeof(DateTime)); // reader(pinnedData, ref index, ref dateTime, context); stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Ldloc(dateTime); // stack: [ref result, ref result, dateTime]
            il.Stfld(Type.GetField(PlatformHelpers.DateTimeOffsetDateTimeFieldName, BindingFlags.Instance | BindingFlags.NonPublic)); // result.m_dateTime = dateTime; stack: [ref result]

            context.LoadData(); // stack: [ref result, data]
            context.LoadIndexByRef(); // stack: [ref result, data, ref index]
            var offset = il.DeclareLocal(typeof(short));
            il.Ldloca(offset); // stack: [ref result, data, ref index, ref offset]
            context.LoadContext(); // stack: [ref result, data, ref index, ref offset, context]
            context.CallReader(typeof(short)); // reader(pinnedData, ref index, ref offset, context); stack: [ref result]
            il.Ldloc(offset); // stack: [ref result, ref result, offset]
            il.Stfld(Type.GetField(PlatformHelpers.DateTimeOffsetOffsetMinutesFieldName, BindingFlags.Instance | BindingFlags.NonPublic)); // result.m_offsetMinutes = offset; stack: [ref result]
        }

        protected override bool IsReference { get { return false; } }
    }
}