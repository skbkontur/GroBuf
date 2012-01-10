using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class SG45
    {
        [DataMember]
        [ProtoMember(1)]
        public PercentageDetails PercentageDetails { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public RangeDetails RangeDetails { get; set; }
    }
}