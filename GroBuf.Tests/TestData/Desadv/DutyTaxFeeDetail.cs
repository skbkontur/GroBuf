using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeDetail
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] DutyTaxFeeRateCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] CodeListIdentificationCode1 { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte[] CodeListResponsibleAgencyCode1 { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public byte[] DutyTaxFeeRate { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public byte[] DutyTaxFeeRateBasisCode { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public byte[] CodeListIdentificationCode2 { get; set; }

        [DataMember]
        [ProtoMember(7)]
        public byte[] CodeListResponsibleAgencyCode2 { get; set; }
    }
}