using System;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class DateTimeReaderBuilder : ReaderBuilderBase
    {
        public DateTimeReaderBuilder()
            : base(typeof(DateTime))
        {
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var constructor = Type.GetConstructor(new[] {typeof(long), typeof(DateTimeKind)});
            if(constructor == null)
                throw new MissingConstructorException(Type, typeof(long), typeof(DateTimeKind));
            var il = context.Il;
            var label = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, context.TypeCode); // stack: [typeCode]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.DateTime); // stack: [typeCode, GroBufTypeCode.DateTime]
            il.Emit(OpCodes.Beq, label); // if(typeCode == GroBufTypeCode.DateTime) goto label

            il.Emit(OpCodes.Ldloc, context.TypeCode); // stack: [typeCode]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.Int64); // stack: [typeCode, GroBufTypeCode.Int64]
            il.Emit(OpCodes.Beq, label); // if(typeCode == GroBufTypeCode.Int64) goto label

            context.SkipValue();
            il.Emit(OpCodes.Ret);

            il.MarkLabel(label);

            context.IncreaseIndexBy1();

            il.Emit(OpCodes.Ldc_I4_8); // stack: [8]
            context.AssertLength();

            context.LoadResultByRef(); // stack: [ref result]

            context.GoToCurrentLocation(); // stack: [ref result, &data[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [ref result, (long)&data[index] = ticks]
            il.Emit(OpCodes.Dup); // stack: [ref result, ticks, ticks]
            il.Emit(OpCodes.Ldc_I8, long.MinValue); // stack: [ref result, ticks, ticks, 0x8000000000000000]
            il.Emit(OpCodes.And); // stack: [ref result, ticks, ticks & 0x8000000000000000]
            var notUtcLabel = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, notUtcLabel); // if(ticks & 0x8000000000000000 != 0) goto notUtc; stack: [ref result, ticks]
            il.Emit(OpCodes.Ldc_I4, (int)DateTimeKind.Utc); // stack: [ref result, ticks, DateTimeKind.Utc]
            il.Emit(OpCodes.Newobj, constructor); // stack: [ref result, new DateTime(ticks, DateTimeKind.Utc)]
            il.Emit(OpCodes.Stobj, Type);
            context.IncreaseIndexBy8();
            il.Emit(OpCodes.Ret);

            il.MarkLabel(notUtcLabel);
            context.IncreaseIndexBy8();
            il.Emit(OpCodes.Ldc_I4_1);
            context.AssertLength();
            il.Emit(OpCodes.Ldc_I8, long.MaxValue); // stack: [ref result, ticks, 0x7FFFFFFFFFFFFFFF]
            il.Emit(OpCodes.And); // stack: [ref result, ticks & 0x7FFFFFFFFFFFFFFF]
            context.GoToCurrentLocation(); // stack: [ref result, ticks & 0x7FFFFFFFFFFFFFFF, &data[index]]
            il.Emit(OpCodes.Ldind_I1); // stack: [ref result, ticks & 0x7FFFFFFFFFFFFFFF, (DateTimeKind)data[index] = kind]
            il.Emit(OpCodes.Newobj, constructor); // stack: [ref result, new DateTime(ticks, kind)]
            il.Emit(OpCodes.Stobj, Type);
            context.IncreaseIndexBy1();
        }
    }
}