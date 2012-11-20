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
            il.Emit(OpCodes.Ldc_I4, (int)DateTimeKind.Utc); // stack: [ref result, ticks, DateTimeKind.Utc]
            var constructor = Type.GetConstructor(new[] {typeof(long), typeof(DateTimeKind)});
            if(constructor == null)
                throw new MissingConstructorException(Type, typeof(long), typeof(DateTimeKind));
            il.Emit(OpCodes.Newobj, constructor); // stack: [ref result, new DateTime(ticks, DateTimeKind.Utc)]
            il.Emit(OpCodes.Stobj, Type);

            context.IncreaseIndexBy8();
        }
    }
}