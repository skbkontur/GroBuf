using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class RateDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public RateDetailsGroup RateDetailsGroup { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public double StatusDescriptionCode { get; set; }
    }
}