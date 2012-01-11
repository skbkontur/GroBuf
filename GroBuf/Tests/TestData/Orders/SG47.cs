using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class SG47
    {
        [DataMember]
        [ProtoMember(1)]
        public RateDetails RateDetails { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public RangeDetails RangeDetails { get; set; }
    }
}