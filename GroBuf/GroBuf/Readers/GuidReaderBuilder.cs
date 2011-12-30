using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class GuidReaderBuilder : ReaderBuilderWithoutParams<Guid>
    {
        public GuidReaderBuilder(IReaderCollection readerCollection)
            : base(readerCollection)
        {
        }

        protected override void ReadNotEmpty(ReaderBuilderContext<Guid> context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCode.Guid); // Assert typeCode == TypeCode.Guid
            var il = context.Il;
            var result = context.Result;

            il.Emit(OpCodes.Ldc_I4, 16);
            context.AssertLength();
            il.Emit(OpCodes.Ldloca, result); // stack: [&result]
            il.Emit(OpCodes.Dup); // stack: [&result, &result]
            context.GoToCurrentLocation(); // stack: [&result, &result, &data[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [&result, &result, (int64)data[index]]
            il.Emit(OpCodes.Stind_I8); // *result = (int64)data[index]; stack: [&result]
            context.IncreaseIndexBy8(); // index = index + 8
            il.Emit(OpCodes.Ldc_I4_8); // stack: [&result, 8]
            il.Emit(OpCodes.Add); // stack: [&result + 8]
            context.GoToCurrentLocation(); // stack: [&result + 8, &data[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [&result + 8, (int64)data[index]]
            il.Emit(OpCodes.Stind_I8); // *(&result + 8) = (int64)data[index]; stack: []
            il.Emit(OpCodes.Ldloc, result); // stack: [result]
            context.IncreaseIndexBy8(); // index = index + 8
        }
    }
}