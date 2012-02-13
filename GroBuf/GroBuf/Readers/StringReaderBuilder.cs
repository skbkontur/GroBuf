using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class StringReaderBuilder : ReaderBuilderBase<string>
    {
        protected override void ReadNotEmpty(ReaderMethodBuilderContext<string> context)
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

            context.LoadResultByRef(); // stack: [ref result]
            context.GoToCurrentLocation(); // stack: [ref result, &data[index]]
            context.Il.Emit(OpCodes.Ldc_I4_0); // stack: [ref result, &data[index], 0]
            context.Il.Emit(OpCodes.Ldloc, length); // stack: [ref result, &data[index], 0, length]
            context.Il.Emit(OpCodes.Ldc_I4_1); // stack: [ref result, &data[index], 0, length, 1]
            context.Il.Emit(OpCodes.Shr_Un); // stack: [ref result, &data[index], 0, length >> 1]
            var constructor = Type.GetConstructor(new[] {typeof(char*), typeof(int), typeof(int)});
            if(constructor == null)
                throw new MissingConstructorException(Type, typeof(char*), typeof(int), typeof(int));
            context.Il.Emit(OpCodes.Newobj, constructor); // stack: [ref result, new string(&data[index], 0, length >> 1)]
            context.Il.Emit(OpCodes.Stind_Ref); // result = new string(&data[index], 0, length >> 1); stack: []
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            context.Il.Emit(OpCodes.Ldloc, length); // stack: [ref index, index, length]
            context.Il.Emit(OpCodes.Add); // stack: [ref index, index + length]
            context.Il.Emit(OpCodes.Stind_I4); // index = index + length; stack: []
        }
    }
}