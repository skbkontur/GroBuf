using System;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class GuidWriterBuilder : WriterBuilderBase<Guid>
    {
        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Emit(OpCodes.Ldc_I4, 17); // stack: [17]
            context.EnsureSize();
            context.WriteTypeCode(GroBufTypeCode.Guid);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.Emit(OpCodes.Ldind_I8); // stack: [&result[index], (int64)*obj]
            il.Emit(OpCodes.Stind_I8); // result[index] = (int64)*obj
            context.IncreaseIndexBy8(); // index = index + 8
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.Emit(OpCodes.Ldc_I4_8); // stack: [&result[index], &obj, 8]
            il.Emit(OpCodes.Add); // stack: [&result[index], &obj + 8]
            il.Emit(OpCodes.Ldind_I8); // stack: [&result[index], *(&obj+8)]
            il.Emit(OpCodes.Stind_I8); // result[index] = (int64)*(obj + 8)
            context.IncreaseIndexBy8(); // index = index + 8
        }
    }
}