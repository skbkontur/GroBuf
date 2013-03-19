using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GroBuf.SizeCounters
{
    internal class DateTimeSizeCounterBuilder : SizeCounterBuilderBase
    {
        public DateTimeSizeCounterBuilder()
            : base(typeof(DateTime))
        {
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(10); // stack: [10]
            context.LoadObjByRef(); // stack: [10, obj]
            il.Call(dateTimeKindProperty.GetGetMethod()); // stack: [10, obj.Kind]
            il.Ldc_I4((int)DateTimeKind.Utc); // stack: [10, obj.Kind, DateTimeKind.Utc]
            il.Ceq(); // stack: [10, obj.Kind == DateTimeKind.Utc]
            il.Sub(); // stack: [10 - (obj.Kind == DateTimeKind.Utc)]
        }

        private static readonly PropertyInfo dateTimeKindProperty = (PropertyInfo)((MemberExpression)((Expression<Func<DateTime, DateTimeKind>>)(dateTime => dateTime.Kind)).Body).Member;
    }
}