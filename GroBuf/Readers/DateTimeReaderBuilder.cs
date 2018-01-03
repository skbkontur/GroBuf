using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit.Utils;

namespace GroBuf.Readers
{
    internal class DateTimeReaderBuilder : ReaderBuilderBase
    {
        public DateTimeReaderBuilder()
            : base(typeof(DateTime))
        {
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var constructor = Type.GetConstructor(new[] {typeof(long), typeof(DateTimeKind)});
            if(constructor == null)
                throw new MissingConstructorException(Type, typeof(long), typeof(DateTimeKind));
            var il = context.Il;

            il.Ldloc(context.TypeCode); // stack: [typeCode]
            il.Ldc_I4((int)GroBufTypeCode.DateTimeNew); // stack: [typeCode, GroBufTypeCode.DateTimeNew]
            var processOldFormatLabel = il.DefineLabel("processOldFormat");
            il.Bne_Un(processOldFormatLabel); // if(typeCode != GroBufTypeCode.DateTimeNew) goto processOldFormat; stack: []
            context.IncreaseIndexBy1();
            il.Ldc_I4(8); // stack: [8]
            context.AssertLength();
            context.LoadResultByRef(); // stack: [ref result]
            context.GoToCurrentLocation(); // stack: [ref result, &data[index]]
            il.Ldind(typeof(long)); // stack: [ref result, (long)data[index]]
            il.Call(dateTimeFromBinaryMethod); // stack: [ref result, DateTime.FromBinary((long)data[index])]
            il.Stobj(Type); // result = DateTime.FromBinary((long)data[index])
            context.IncreaseIndexBy8(); // index = index + 8
            il.Ret();

            il.MarkLabel(processOldFormatLabel);
            var okLabel = il.DefineLabel("ok");
            il.Ldloc(context.TypeCode); // stack: [typeCode]
            il.Ldc_I4((int)GroBufTypeCode.DateTimeOld); // stack: [typeCode, GroBufTypeCode.DateTimeOld]
            il.Beq(okLabel); // if(typeCode == GroBufTypeCode.DateTimeOld) goto label

            il.Ldloc(context.TypeCode); // stack: [typeCode]
            il.Ldc_I4((int)GroBufTypeCode.Int64); // stack: [typeCode, GroBufTypeCode.Int64]
            il.Beq(okLabel); // if(typeCode == GroBufTypeCode.Int64) goto label

            context.SkipValue();
            il.Ret();

            il.MarkLabel(okLabel);

            context.IncreaseIndexBy1();

            il.Ldc_I4(8); // stack: [8]
            context.AssertLength();

            context.LoadResultByRef(); // stack: [ref result]

            context.GoToCurrentLocation(); // stack: [ref result, &data[index]]
            il.Ldind(typeof(long)); // stack: [ref result, (long)&data[index] = ticks]
            il.Dup(); // stack: [ref result, ticks, ticks]
            il.Ldc_I8(long.MinValue); // stack: [ref result, ticks, ticks, 0x8000000000000000]
            il.And(); // stack: [ref result, ticks, ticks & 0x8000000000000000]
            var notUtcLabel = il.DefineLabel("notUtc");
            il.Brtrue(notUtcLabel); // if(ticks & 0x8000000000000000 != 0) goto notUtc; stack: [ref result, ticks]
            il.Ldc_I4((int)DateTimeKind.Utc); // stack: [ref result, ticks, DateTimeKind.Utc]
            il.Newobj(constructor); // stack: [ref result, new DateTimeOld(ticks, DateTimeKind.Utc)]
            il.Stobj(Type);
            context.IncreaseIndexBy8();
            il.Ret();

            il.MarkLabel(notUtcLabel);
            context.IncreaseIndexBy8();
            il.Ldc_I4(1);
            context.AssertLength();
            il.Ldc_I8(long.MaxValue); // stack: [ref result, ticks, 0x7FFFFFFFFFFFFFFF]
            il.And(); // stack: [ref result, ticks & 0x7FFFFFFFFFFFFFFF]
            context.GoToCurrentLocation(); // stack: [ref result, ticks & 0x7FFFFFFFFFFFFFFF, &data[index]]
            il.Ldind(typeof(byte)); // stack: [ref result, ticks & 0x7FFFFFFFFFFFFFFF, (DateTimeKind)data[index] = kind]
            il.Newobj(constructor); // stack: [ref result, new DateTimeOld(ticks, kind)]
            il.Stobj(Type);
            context.IncreaseIndexBy1();
        }

        protected override bool IsReference { get { return false; } }

        private static readonly MethodInfo dateTimeFromBinaryMethod = HackHelpers.GetMethodDefinition((Expression<Func<long, object>>)(data => DateTime.FromBinary(data)));
    }
}