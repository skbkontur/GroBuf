using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
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