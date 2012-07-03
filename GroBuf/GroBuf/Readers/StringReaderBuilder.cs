using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class StringReaderBuilder : ReaderBuilderBase<string>
    {
        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1(); // Skip typeCode
            context.AssertTypeCode(GroBufTypeCode.String); // Assert typeCode == TypeCode.String

            var il = context.Il;
            il.Emit(OpCodes.Ldc_I4_4);
            context.AssertLength();

            var length = context.Length;

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [(uint)data[index]]
            il.Emit(OpCodes.Dup); // stack: [(uint)data[index], (uint)data[index]]
            il.Emit(OpCodes.Stloc, length); // length = (uint)data[index]; stack: [length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [length]

            var stringIsEmptyLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, stringIsEmptyLabel);

            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            context.AssertLength();

            context.LoadResultByRef(); // stack: [ref result]
            context.GoToCurrentLocation(); // stack: [ref result, &data[index]]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [ref result, &data[index], 0]
            il.Emit(OpCodes.Ldloc, length); // stack: [ref result, &data[index], 0, length]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [ref result, &data[index], 0, length, 1]
            il.Emit(OpCodes.Shr_Un); // stack: [ref result, &data[index], 0, length >> 1]
            var constructor = Type.GetConstructor(new[] { typeof(char*), typeof(int), typeof(int) });
            if (constructor == null)
                throw new MissingConstructorException(Type, typeof(char*), typeof(int), typeof(int));
            il.Emit(OpCodes.Newobj, constructor); // stack: [ref result, new string(&data[index], 0, length >> 1)]
            il.Emit(OpCodes.Stind_Ref); // result = new string(&data[index], 0, length >> 1); stack: []
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Emit(OpCodes.Ldloc, length); // stack: [ref index, index, length]
            il.Emit(OpCodes.Add); // stack: [ref index, index + length]
            il.Emit(OpCodes.Stind_I4); // index = index + length; stack: []
            il.Emit(OpCodes.Ret);

            il.MarkLabel(stringIsEmptyLabel);
            context.LoadResultByRef(); // stack: [ref result]
            il.Emit(OpCodes.Ldstr, ""); // stack: [ref result, ""]
            il.Emit(OpCodes.Stind_Ref); // result = ""; stack: []
        }
    }
}