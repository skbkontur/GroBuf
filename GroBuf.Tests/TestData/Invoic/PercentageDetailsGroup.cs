using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class PercentageDetailsGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public string PercentageTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public string Percentage { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string PercentageBasisIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public string CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public string CodeListResponsibleAgencyCode { get; set; }
    }
}