namespace GroBuf.Readers
{
    internal class StringReaderBuilder : ReaderBuilderBase
    {
        public StringReaderBuilder()
            : base(typeof(string))
        {
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1(); // Skip typeCode
            context.AssertTypeCode(GroBufTypeCode.String); // Assert typeCode == TypeCode.String

            var il = context.Il;
            il.Ldc_I4(4);
            context.AssertLength();

            var length = context.Length;

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [(uint)data[index]]
            il.Dup(); // stack: [(uint)data[index], (uint)data[index]]
            il.Stloc(length); // length = (uint)data[index]; stack: [length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [length]

            var stringIsEmptyLabel = il.DefineLabel("stringIsEmpty");
            il.Brfalse(stringIsEmptyLabel);

            il.Ldloc(length); // stack: [length]
            context.AssertLength();

            context.LoadResultByRef(); // stack: [ref result]
            context.GoToCurrentLocation(); // stack: [ref result, &data[index]]
            il.Ldc_I4(0); // stack: [ref result, &data[index], 0]
            il.Ldloc(length); // stack: [ref result, &data[index], 0, length]
            il.Ldc_I4(1); // stack: [ref result, &data[index], 0, length, 1]
            il.Shr(true); // stack: [ref result, &data[index], 0, length >> 1]
            var constructor = Type.GetConstructor(new[] {typeof(char*), typeof(int), typeof(int)});
            if(constructor == null)
                throw new MissingConstructorException(Type, typeof(char*), typeof(int), typeof(int));
            il.Newobj(constructor); // stack: [ref result, new string(&data[index], 0, length >> 1)]
            il.Stind(typeof(string)); // result = new string(&data[index], 0, length >> 1); stack: []

            context.StoreObject(Type);

            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(length); // stack: [ref index, index, length]
            il.Add(); // stack: [ref index, index + length]
            il.Stind(typeof(int)); // index = index + length; stack: []
            il.Ret();

            il.MarkLabel(stringIsEmptyLabel);
            context.LoadResultByRef(); // stack: [ref result]
            il.Ldstr(""); // stack: [ref result, ""]
            il.Stind(typeof(string)); // result = ""; stack: []
        }

        protected override bool IsReference { get { return true; } }
    }
}