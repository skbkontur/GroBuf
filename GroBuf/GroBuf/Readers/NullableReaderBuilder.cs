using System;

namespace GroBuf.Readers
{
    internal class NullableReaderBuilder : ReaderBuilderBase
    {
        public NullableReaderBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                throw new InvalidOperationException("Expected nullable but was '" + Type + "'");
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;
            context.LoadResultByRef(); // stack: [ref result]
            context.LoadData(); // stack: [ref result, data]
            context.LoadIndexByRef(); // stack: [ref result, data, ref index]
            context.LoadDataLength(); // stack: [ref result, data, ref index, dataLength]
            var elementType = Type.GetGenericArguments()[0];
            var value = il.DeclareLocal(elementType);
            il.Ldloca(value); // stack: [ref result, data, ref index, dataLength, ref value]
            il.Call(context.Context.GetReader(elementType)); // reader(pinnedData, ref index, dataLength, ref value); stack: [ref result]
            il.Ldloc(value); // stack: [ref result, value]
            var constructor = Type.GetConstructor(new[] {elementType});
            if(constructor == null)
                throw new MissingConstructorException(Type, elementType);
            il.Newobj(constructor); // stack: [ref result, new elementType?(value)]
            il.Stobj(Type); // result = new elementType?(value)
        }
    }
}