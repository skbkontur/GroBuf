using System;

namespace GroBuf.Readers
{
    internal class GuidReaderBuilder : ReaderBuilderBase
    {
        public GuidReaderBuilder()
            : base(typeof(Guid))
        {
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCode.Guid); // Assert typeCode == TypeCode.Guid
            var il = context.Il;
            var pinnedResult = il.DeclareLocal(Type.MakeByRefType(), true);

            il.Ldc_I4(16);
            context.AssertLength();
            context.LoadResultByRef(); // stack: [ref result]
            il.Stloc(pinnedResult); // pinnedResult = ref result
            il.Ldloc(pinnedResult); // stack: [&result]
            il.Dup(); // stack: [&result, &result]
            context.GoToCurrentLocation(); // stack: [&result, &result, &data[index]]
            il.Ldind(typeof(long)); // stack: [&result, &result, (int64)data[index]]
            il.Stind(typeof(long)); // *result = (int64)data[index]; stack: [&result]
            context.IncreaseIndexBy8(); // index = index + 8
            il.Ldc_I4(8); // stack: [&result, 8]
            il.Add(); // stack: [&result + 8]
            context.GoToCurrentLocation(); // stack: [&result + 8, &data[index]]
            il.Ldind(typeof(long)); // stack: [&result + 8, (int64)data[index]]
            il.Stind(typeof(long)); // *(&result + 8) = (int64)data[index]; stack: []
            context.IncreaseIndexBy8(); // index = index + 8
            il.Ldc_I4(0); // stack: [0]
            il.Conv_U(); // stack: [null]
            il.Stloc(pinnedResult); // pinnedResult = null
        }

        protected override bool IsReference { get { return false; } }
    }
}