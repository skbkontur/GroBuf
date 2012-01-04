using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class NullableReaderBuilder<T> : ReaderBuilderBase<T>
    {
        public NullableReaderBuilder()
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                throw new InvalidOperationException("Expected nullable but was " + Type);
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext<T> context)
        {
            var il = context.Il;
            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            var elementType = Type.GetGenericArguments()[0];
            il.Emit(OpCodes.Call, context.Context.GetReader(elementType)); // stack: [reader(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Newobj, Type.GetConstructor(new[] {elementType})); // stack: new type(reader(pinnedData, ref index, dataLength))
        }
    }
}