using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public string DutyTaxFeeFunctionCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public DutyTaxFeeType DutyTaxFeeType { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public DutyTaxFeeAccountDetail DutyTaxFeeAccountDetail { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public string DutyTaxFeeAssessmentBasisValue { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public DutyTaxFeeDetail DutyTaxFeeDetail { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public string DutyTaxFeeCategoryCode { get; set; }

        [DataMember]
        [ProtoMember(7)]
        public string PartyTaxIdentifier { get; set; }

        [DataMember]
        [ProtoMember(8)]
        public string CalculationSequenceCode { get; set; }
    }
}