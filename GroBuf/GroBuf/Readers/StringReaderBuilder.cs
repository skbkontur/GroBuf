using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class StringReaderBuilder : ReaderBuilderWithoutParams<string>
    {
        public StringReaderBuilder(IReaderCollection readerCollection)
            : base(readerCollection)
        {
        }

        protected override void ReadNotEmpty(ReaderBuilderContext<string> context)
        {
            context.IncreaseIndexBy1(); // Skip typeCode
            context.AssertTypeCode(GroBufTypeCode.String); // Assert typeCode == TypeCode.String

            context.Il.Emit(OpCodes.Ldc_I4_4);
            context.AssertLength();

            var length = context.Length;

            context.GoToCurrentLocation(); // stack: [&data[index]]
            context.Il.Emit(OpCodes.Ldind_U4); // stack: [(uint)data[index]]
            context.Il.Emit(OpCodes.Dup); // stack: [(uint)data[index], (uint)data[index]]
            context.Il.Emit(OpCodes.Stloc, length); // length = (uint)data[index]; stack: [length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [length]

            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            context.Il.Emit(OpCodes.Ldc_I4_0); // stack: [&data[index], 0]
            context.Il.Emit(OpCodes.Ldloc, length); // stack: [&data[index], 0, length]
            context.Il.Emit(OpCodes.Ldc_I4_1); // stack: [&data[index], 0, length, 1]
            context.Il.Emit(OpCodes.Shr_Un); // stack: [&data[index], 0, length >> 1]
            context.Il.Emit(OpCodes.Newobj, typeof(string).GetConstructor(new[] {typeof(char*), typeof(int), typeof(int)})); // stack: [new string(&data[index], 0, length >> 1)]
            context.LoadIndexByRef(); // stack: [new string(&data[index], 0, length >> 1), ref index]
            context.LoadIndex(); // stack: [new string(&data[index], 0, length >> 1), ref index, index]
            context.Il.Emit(OpCodes.Ldloc, length); // stack: [new string(&data[index], 0, length >> 1), ref index, index, length]
            context.Il.Emit(OpCodes.Add); // stack: [new string(&data[index], 0, length >> 1), ref index, index + length]
            context.Il.Emit(OpCodes.Stind_I4); // index = index + length; stack: [new string(&data[index], 0, length >> 1)]
        }
    }
}