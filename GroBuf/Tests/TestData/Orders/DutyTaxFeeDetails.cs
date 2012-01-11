using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public int[] DutyTaxFeeFunctionCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public DutyTaxFeeType DutyTaxFeeType { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public DutyTaxFeeAccountDetail DutyTaxFeeAccountDetail { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public ulong?[] DutyTaxFeeAssessmentBasisValue { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public DutyTaxFeeDetail DutyTaxFeeDetail { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public byte[] DutyTaxFeeCategoryCode { get; set; }

        [DataMember]
        [ProtoMember(7)]
        public sbyte[] PartyTaxIdentifier { get; set; }

        [DataMember]
        [ProtoMember(8)]
        public sbyte?[] CalculationSequenceCode { get; set; }
    }
}