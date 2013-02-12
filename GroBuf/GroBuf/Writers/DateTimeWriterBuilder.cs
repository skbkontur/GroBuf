using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class DateTimeWriterBuilder : WriterBuilderBase
    {
        public DateTimeWriterBuilder()
            : base(typeof(DateTime))
        {
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.DateTime);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.EmitCall(OpCodes.Call, dateTimeKindProperty.GetGetMethod(), null); // stack: [&result[index], obj.Kind]
            var kind = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Dup); // stack: [&result[index], obj.Kind, obj.Kind]
            il.Emit(OpCodes.Stloc, kind); // kind = obj.Kind; stack: [&result[index], obj.Kind]
            il.Emit(OpCodes.Ldc_I4_S, (byte)DateTimeKind.Utc); // stack: [&result[index], obj.Kind, DateTimeKind.Utc]
            var notUtcLabel = il.DefineLabel();
            il.Emit(OpCodes.Bne_Un, notUtcLabel); // if(obj.Kind != DateTimeKind.Utc) goto notUtc; stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.EmitCall(OpCodes.Call, dateTimeTicksProperty.GetGetMethod(), null); // stack: [&result[index], obj.Ticks]
            il.Emit(OpCodes.Stind_I8); // (long)&result[index] = ticks
            context.IncreaseIndexBy8(); // index = index + 8
            il.Emit(OpCodes.Ret);
            il.MarkLabel(notUtcLabel);
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.EmitCall(OpCodes.Call, dateTimeTicksProperty.GetGetMethod(), null); // stack: [&result[index], obj.Ticks]
            il.Emit(OpCodes.Ldc_I8, long.MinValue); // stack: [&result[index], obj.Ticks, 0x8000000000000000]
            il.Emit(OpCodes.Or); // stack: [&result[index], obj.Ticks | 0x8000000000000000]
            il.Emit(OpCodes.Stind_I8); // (long)&result[index] = ticks | 0x8000000000000000
            context.IncreaseIndexBy8(); // index = index + 8
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Emit(OpCodes.Ldloc, kind); // stack: [&result[index], kind]
            il.Emit(OpCodes.Stind_I1); // &result[index] = (byte)kind;
            context.IncreaseIndexBy1(); // index = index + 1
        }

        private static readonly PropertyInfo dateTimeKindProperty = (PropertyInfo)((MemberExpression)((Expression<Func<DateTime, DateTimeKind>>)(dateTime => dateTime.Kind)).Body).Member;
        private static readonly PropertyInfo dateTimeTicksProperty = (PropertyInfo)((MemberExpression)((Expression<Func<DateTime, long>>)(dateTime => dateTime.Ticks)).Body).Member;
    }
}