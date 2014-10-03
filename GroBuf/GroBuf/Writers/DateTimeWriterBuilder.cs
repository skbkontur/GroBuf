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

/*
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.DateTimeOld);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.Call(dateTimeKindProperty.GetGetMethod()); // stack: [&result[index], obj.Kind]
            var kind = il.DeclareLocal(typeof(int));
            il.Dup(); // stack: [&result[index], obj.Kind, obj.Kind]
            il.Stloc(kind); // kind = obj.Kind; stack: [&result[index], obj.Kind]
            il.Ldc_I4((int)DateTimeKind.Utc); // stack: [&result[index], obj.Kind, DateTimeKind.Utc]
            var notUtcLabel = il.DefineLabel("notUtc");
            il.Bne(notUtcLabel); // if(obj.Kind != DateTimeKind.Utc) goto notUtc; stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.Call(dateTimeTicksProperty.GetGetMethod()); // stack: [&result[index], obj.Ticks]
            il.Stind(typeof(long)); // (long)&result[index] = ticks
            context.IncreaseIndexBy8(); // index = index + 8
            il.Ret();
            il.MarkLabel(notUtcLabel);
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.Call(dateTimeTicksProperty.GetGetMethod()); // stack: [&result[index], obj.Ticks]
            il.Ldc_I8(long.MinValue); // stack: [&result[index], obj.Ticks, 0x8000000000000000]
            il.Or(); // stack: [&result[index], obj.Ticks | 0x8000000000000000]
            il.Stind(typeof(long)); // (long)&result[index] = ticks | 0x8000000000000000
            context.IncreaseIndexBy8(); // index = index + 8
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(kind); // stack: [&result[index], kind]
            il.Stind(typeof(byte)); // &result[index] = (byte)kind;
            context.IncreaseIndexBy1(); // index = index + 1
*/
        }

        protected override bool IsReference { get { return false; } }

        private static readonly PropertyInfo dateTimeKindProperty = (PropertyInfo)((MemberExpression)((Expression<Func<DateTime, DateTimeKind>>)(dateTime => dateTime.Kind)).Body).Member;
        private static readonly PropertyInfo dateTimeTicksProperty = (PropertyInfo)((MemberExpression)((Expression<Func<DateTime, long>>)(dateTime => dateTime.Ticks)).Body).Member;
        private static readonly MethodInfo dateTimeToBinaryMethod = ((MethodCallExpression)((Expression<Func<DateTime, long>>)(dateTime => dateTime.ToBinary())).Body).Method;
    }
}