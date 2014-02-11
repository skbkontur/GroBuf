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
            context.LoadDataLength(); // stack: [ref result, data, ref index, dataLength]
            var value = il.DeclareLocal(typeof(byte[]));
            il.Ldloca(value); // stack: [ref result, data, ref index, dataLength, ref value]
            context.CallReader(typeof(byte[])); // reader(pinnedData, ref index, dataLength, ref value); stack: [ref result]
            il.Ldloc(value); // stack: [ref result, value]
            var constructor = Type.GetConstructor(new[] {typeof(byte[])});
            if(constructor == null)
                throw new MissingConstructorException(Type, typeof(byte[]));
            il.Newobj(constructor); // stack: [ref result, new IPAddress(value)]
            il.Stind(Type); // result = new IPAddress(value)
        }
    }
}