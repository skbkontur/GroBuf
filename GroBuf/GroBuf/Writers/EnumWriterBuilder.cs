using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal class EnumWriterBuilder<T> : WriterBuilderWithOneParam<T, ulong[]>
    {
        public EnumWriterBuilder(IWriterCollection writerCollection)
            : base(writerCollection)
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was " + Type);
        }

        protected override ulong[] WriteNotEmpty(WriterBuilderContext context)
        {
            context.Il.Emit(OpCodes.Ldc_I4, 9); // stack: [9]
            context.EnsureSize();
            context.WriteTypeCode(GroBufTypeCode.Enum);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadAdditionalParam(0); // stack: [&result[index], hashCodes]
            context.LoadObj(); // stack: [&result[index], hashCodes, obj]
            context.Il.Emit(OpCodes.Ldelem_I8); // stack: [&result[index], hashCodes[obj]]
            context.Il.Emit(OpCodes.Stind_I8); // *(int64*)&result[index] = hashCodes[obj]
            context.IncreaseIndexBy8();
            return BuildHashCodes();
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