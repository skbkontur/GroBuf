using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeAccountDetail
    {
        [DataMember]
        [ProtoMember(1)]
        public short? DutyTaxFeeAccountCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public ushort? CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public char CodeListResponsibleAgencyCode { get; set; }
    }
}