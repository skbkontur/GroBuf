using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GroBuf.Writers
{
    internal class DateTimeWriterBuilder : WriterBuilderBase
    {
        public DateTimeWriterBuilder()
            : base(typeof(DateTime))
        {
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.DateTimeNew);
            il.Ldc_I4(8);
            context.AssertLength();
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.Call(dateTimeToBinaryMethod, Type); // stack: [&result[index], obj.ToBinary()]
            il.Stind(typeof(long)); // result[index] = obj.ToBinary()
            context.IncreaseIndexBy8(); // index = index + 8
        }

        protected override bool IsReference { get { return false; } }

        private static readonly MethodInfo dateTimeToBinaryMethod = ((MethodCallExpression)((Expression<Func<DateTime, long>>)(dateTime => dateTime.ToBinary())).Body).Method;
    }
}