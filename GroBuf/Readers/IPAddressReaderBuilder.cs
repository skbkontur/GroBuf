using System.Net;

namespace GroBuf.Readers
{
    internal class IPAddressReaderBuilder : ReaderBuilderBase
    {
        public IPAddressReaderBuilder()
            : base(typeof(IPAddress))
        {
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(typeof(byte[]));
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;
            context.LoadResultByRef(); // stack: [ref result]

            context.LoadData(); // stack: [ref result, data]
            context.LoadIndexByRef(); // stack: [ref result, data, ref index]
            var value = il.DeclareLocal(typeof(byte[]));
            il.Ldloca(value); // stack: [ref result, data, ref index, ref value]
            context.LoadContext(); // stack: [ref result, data, ref index, ref value, context]
            context.CallReader(typeof(byte[])); // reader(pinnedData, ref index, ref value, context); stack: [ref result]
            il.Ldloc(value); // stack: [ref result, value]
            var constructor = Type.GetConstructor(new[] {typeof(byte[])});
            if (constructor == null)
                throw new MissingConstructorException(Type, typeof(byte[]));
            il.Newobj(constructor); // stack: [ref result, new IPAddress(value)]
            il.Stind(Type); // result = new IPAddress(value)
        }

        protected override bool IsReference => false;
    }
}