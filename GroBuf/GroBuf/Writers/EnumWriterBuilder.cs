using System;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class EnumWriterBuilder<T> : WriterBuilderBase<T>
    {
        public EnumWriterBuilder()
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was " + Type);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var hashCodesField = context.Context.BuildConstField("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), BuildHashCodes());
            context.Il.Emit(OpCodes.Ldc_I4, 9); // stack: [9]
            context.EnsureSize();
            context.WriteTypeCode(GroBufTypeCode.Enum);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadField(hashCodesField); // stack: [&result[index], hashCodes]
            context.LoadObj(); // stack: [&result[index], hashCodes, obj]
            context.Il.Emit(OpCodes.Ldelem_I8); // stack: [&result[index], hashCodes[obj]]
            context.Il.Emit(OpCodes.Stind_I8); // *(int64*)&result[index] = hashCodes[obj]
            context.IncreaseIndexBy8();
        }

        private ulong[] BuildHashCodes()
        {
            var values = Enum.GetValues(Type);
            var names = Enum.GetNames(Type);
            var hashCodes = new ulong[values.Length];
            for(int i = 0; i < values.Length; ++i)
                hashCodes[(int)values.GetValue(i)] = GroBufHelpers.CalcHash(names[i]);
            return hashCodes;
        }
    }
}