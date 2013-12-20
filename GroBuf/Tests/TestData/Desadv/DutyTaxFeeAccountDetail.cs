using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeAccountDetail
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] DutyTaxFeeAccountCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte[] CodeListResponsibleAgencyCode { get; set; }
    }
}