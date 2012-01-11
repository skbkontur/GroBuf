using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class Quantity
    {
        [DataMember]
        [ProtoMember(1)]
        public QuantityDetails QuantityDetails { get; set; }
    }
}