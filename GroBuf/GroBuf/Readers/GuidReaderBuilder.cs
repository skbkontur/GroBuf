using System;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class GuidReaderBuilder : ReaderBuilderBase<Guid>
    {
        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCode.Guid); // Assert typeCode == TypeCode.Guid
            var il = context.Il;
            var pinnedResult = il.DeclareLocal(Type.MakeByRefType(), true);

            il.Emit(OpCodes.Ldc_I4, 16);
            context.AssertLength();
            context.LoadResultByRef(); // stack: [ref result]
            il.Emit(OpCodes.Stloc, pinnedResult); // pinnedResult = ref result
            il.Emit(OpCodes.Ldloc, pinnedResult); // stack: [&result]
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
            context.IncreaseIndexBy8(); // index = index + 8
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Conv_U); // stack: [null]
            il.Emit(OpCodes.Stloc, pinnedResult); // pinnedResult = null
        }
    }
}