using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class DateTimeSizeCounterBuilder : SizeCounterBuilderBase
    {
        public DateTimeSizeCounterBuilder()
            : base(typeof(DateTime))
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Emit(OpCodes.Ldc_I4, 10); // stack: [10]
            context.LoadObjByRef(); // stack: [10, obj]
            il.EmitCall(OpCodes.Call, dateTimeKindProperty.GetGetMethod(), null); // stack: [10, obj.Kind]
            il.Emit(OpCodes.Ldc_I4_S, (byte)DateTimeKind.Utc); // stack: [10, obj.Kind, DateTimeKind.Utc]
            il.Emit(OpCodes.Ceq); // stack: [10, obj.Kind == DateTimeKind.Utc]
            il.Emit(OpCodes.Sub); // stack: [10 - (obj.Kind == DateTimeKind.Utc)]
        }

        private static readonly PropertyInfo dateTimeKindProperty = (PropertyInfo)((MemberExpression)((Expression<Func<DateTime, DateTimeKind>>)(dateTime => dateTime.Kind)).Body).Member;
    }
}