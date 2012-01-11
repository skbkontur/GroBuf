using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
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