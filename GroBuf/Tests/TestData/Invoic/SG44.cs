using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class SG44
    {
        [DataMember]
        [ProtoMember(1)]
        public Quantity Quantity { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public RangeDetails RangeDetails { get; set; }
    }
}