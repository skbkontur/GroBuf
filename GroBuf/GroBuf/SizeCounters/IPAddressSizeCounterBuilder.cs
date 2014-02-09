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

//            context.LoadSizeCounter(Type.GetGenericArguments()[0]);

            il.Ldc_I4(4); // stack: [4]
            context.LoadObj(); // stack: [4, obj]
            il.Ldfld(Type.GetField("m_Family", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [4, obj.m_Family]
            il.Ldc_I4((int)AddressFamily.InterNetworkV6); // stack: [4, obj.m_Family, AddressFamily.InterNetworkV6]
            il.Ceq(); // stack: [4, obj.m_Family == AddressFamily.InterNetworkV6]
            il.Ldc_I4(1); // stack: [4, obj.m_Family == AddressFamily.InterNetworkV6, 1]
            il.Shl(); // stack: [4, (obj.m_Family == AddressFamily.InterNetworkV6) << 1]
            il.Shl(); // stack: [4 << ((obj.m_Family == AddressFamily.InterNetworkV6) << 1)]
            il.Ldc_I4(5); // stack: [4 << ((obj.m_Family == AddressFamily.InterNetworkV6) << 1), 5]
            il.Add(); // stack: [4 << ((obj.m_Family == AddressFamily.InterNetworkV6) << 1) + 5]
        }
    }
}