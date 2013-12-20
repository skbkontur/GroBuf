using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class PercentageDetailsGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] PercentageTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] Percentage { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte[] PercentageBasisIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public byte[] CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public byte[] CodeListResponsibleAgencyCode { get; set; }
    }
}