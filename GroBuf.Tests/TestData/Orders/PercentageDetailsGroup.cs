using System;
using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class PercentageDetailsGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public uint[] PercentageTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public DateTime? Percentage { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public float PercentageBasisIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public float? CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public string CodeListResponsibleAgencyCode { get; set; }
    }
}