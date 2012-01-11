using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeType
    {
        [DataMember]
        [ProtoMember(1)]
        public string DutyTaxFeeTypeNameCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public string CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string CodeListResponsibleAgencyCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public string DutyTaxFeeTypeName { get; set; }
    }
}