using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class NullableReaderBuilder<T> : ReaderBuilderWithOneParam<T, Delegate>
    {
        public NullableReaderBuilder(IReaderCollection readerCollection)
            : base(readerCollection)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                throw new InvalidOperationException("Expected nullable but was " + Type);
        }

        protected override Delegate ReadNotEmpty(ReaderBuilderContext<T> context)
        {
            var il = context.Il;
            context.LoadAdditionalParam(0); // stack: [reader]
            context.LoadData(); // stack: [reader, pinnedData]
            context.LoadIndexByRef(); // stack: [reader, pinnedData, ref index]
            context.LoadDataLength(); // stack: [reader, pinnedData, ref index, dataLength]
            var elementType = Type.GetGenericArguments()[0];
            var reader = GetReader(elementType);
            il.Emit(OpCodes.Call, reader.GetType().GetMethod("Invoke")); // stack: [reader(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Newobj, Type.GetConstructor(new[] {elementType})); // stack: new type(reader(pinnedData, ref index, dataLength))
            return reader;
        }
    }
}