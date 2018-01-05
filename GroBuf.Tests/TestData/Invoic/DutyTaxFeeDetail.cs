using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeDetail
    {
        [DataMember]
        [ProtoMember(1)]
        public string DutyTaxFeeRateCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public string CodeListIdentificationCode1 { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string CodeListResponsibleAgencyCode1 { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public string DutyTaxFeeRate { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public string DutyTaxFeeRateBasisCode { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public string CodeListIdentificationCode2 { get; set; }

        [DataMember]
        [ProtoMember(7)]
        public string CodeListResponsibleAgencyCode2 { get; set; }
    }
}