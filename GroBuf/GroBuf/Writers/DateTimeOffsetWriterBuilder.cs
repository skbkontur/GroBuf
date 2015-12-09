using System;
using System.Reflection;

namespace GroBuf.Writers
{
    internal class DateTimeOffsetWriterBuilder : WriterBuilderBase
    {
        public DateTimeOffsetWriterBuilder()
            : base(typeof(DateTimeOffset))
        {
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(typeof(DateTime));
            context.BuildConstants(typeof(short));
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;

            context.WriteTypeCode(GroBufTypeCode.DateTimeOffset);
            context.LoadObjByRef(); // stack: [obj]
            il.Ldfld(Type.GetField("m_dateTime", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.m_dateTime]
            context.LoadWriteEmpty(); // stack: [obj.m_dateTime, writeEmpty]
            context.LoadResult(); // stack: [obj.m_dateTime, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj.m_dateTime, writeEmpty, result, ref index]
            context.LoadContext(); // stack: [obj.m_dateTime, writeEmpty, result, ref index, context]
            context.CallWriter(typeof(DateTime)); // writer(obj.m_dateTime, writeEmpty, result, ref index, context)

            context.LoadObjByRef(); // stack: [obj]
            il.Ldfld(Type.GetField("m_offsetMinutes", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.m_offsetMinutes]
            context.LoadWriteEmpty(); // stack: [obj.m_offsetMinutes, writeEmpty]
            context.LoadResult(); // stack: [obj.m_offsetMinutes, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj.m_offsetMinutes, writeEmpty, result, ref index]
            context.LoadContext(); // stack: [obj.m_offsetMinutes, writeEmpty, result, ref index, context]
            context.CallWriter(typeof(short)); // writer(obj.m_offsetMinutes, writeEmpty, result, ref index, context)
        }

        protected override bool IsReference { get { return false; } }
    }
}