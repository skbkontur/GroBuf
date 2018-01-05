using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace GroBuf.SizeCounters
{
    internal class IPAddressSizeCounterBuilder : SizeCounterBuilderBase
    {
        public IPAddressSizeCounterBuilder()
            : base(typeof(IPAddress))
        {
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            il.Ldc_I4(4); // stack: [4]
            context.LoadObj(); // stack: [4, obj]
            il.Call(Type.GetProperty("AddressFamily", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()); // stack: [4, obj.AddressFamily]
            il.Ldc_I4((int)AddressFamily.InterNetworkV6); // stack: [4, obj.AddressFamily, AddressFamily.InterNetworkV6]
            il.Ceq(); // stack: [4, obj.AddressFamily == AddressFamily.InterNetworkV6]
            il.Ldc_I4(1); // stack: [4, obj.AddressFamily == AddressFamily.InterNetworkV6, 1]
            il.Shl(); // stack: [4, (obj.AddressFamily == AddressFamily.InterNetworkV6) << 1]
            il.Shl(); // stack: [4 << ((obj.AddressFamily == AddressFamily.InterNetworkV6) << 1)]
            il.Ldc_I4(5); // stack: [4 << ((obj.AddressFamily == AddressFamily.InterNetworkV6) << 1), 5]
            il.Add(); // stack: [4 << ((obj.AddressFamily == AddressFamily.InterNetworkV6) << 1) + 5]
        }

        protected override bool IsReference { get { return false; } }
    }
}