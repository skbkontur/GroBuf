using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] DutyTaxFeeFunctionCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public DutyTaxFeeType DutyTaxFeeType { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public DutyTaxFeeAccountDetail DutyTaxFeeAccountDetail { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public byte[] DutyTaxFeeAssessmentBasisValue { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public DutyTaxFeeDetail DutyTaxFeeDetail { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public byte[] DutyTaxFeeCategoryCode { get; set; }

        [DataMember]
        [ProtoMember(7)]
        public byte[] PartyTaxIdentifier { get; set; }

        [DataMember]
        [ProtoMember(8)]
        public byte[] CalculationSequenceCode { get; set; }
    }
}