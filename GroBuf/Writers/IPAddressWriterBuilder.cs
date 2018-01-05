using System.Net;
using System.Reflection;

namespace GroBuf.Writers
{
    internal class IPAddressWriterBuilder : WriterBuilderBase
    {
        public IPAddressWriterBuilder()
            : base(typeof(IPAddress))
        {
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(typeof(byte[]));
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;

            context.LoadObj(); // stack: [obj]
            il.Call(Type.GetMethod("GetAddressBytes", BindingFlags.Instance | BindingFlags.Public)); // stack: [obj.GetAddressBytes()]
            context.LoadWriteEmpty(); // stack: [obj.GetAddressBytes(), writeEmpty]
            context.LoadResult(); // stack: [obj.GetAddressBytes(), writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj.GetAddressBytes(), writeEmpty, result, ref index]
            context.LoadContext(); // stack: [obj.GetAddressBytes(), writeEmpty, result, ref index, context]
            context.CallWriter(typeof(byte[])); // writer(obj.GetAddressBytes(), writeEmpty, result, ref index, context)
        }

        protected override bool IsReference { get { return false; } }
    }
}