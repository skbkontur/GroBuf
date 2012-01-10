using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class SG48
    {
        [DataMember]
        [ProtoMember(1)]
        public DutyTaxFeeDetails DutyTaxFeeDetails { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public MonetaryAmount MonetaryAmount { get; set; }
    }
}