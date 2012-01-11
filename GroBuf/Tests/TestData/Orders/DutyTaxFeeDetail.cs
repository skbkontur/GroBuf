using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeDetail
    {
        [DataMember]
        [ProtoMember(1)]
        public char? DutyTaxFeeRateCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public int?[] CodeListIdentificationCode1 { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public uint? CodeListResponsibleAgencyCode1 { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public long? DutyTaxFeeRate { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public ulong DutyTaxFeeRateBasisCode { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public long CodeListIdentificationCode2 { get; set; }

        [DataMember]
        [ProtoMember(7)]
        public ulong? CodeListResponsibleAgencyCode2 { get; set; }
    }
}