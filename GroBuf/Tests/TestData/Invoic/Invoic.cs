using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class Invoic
    {
        [DataMember]
        [ProtoMember(1)]
        public AllowanceOrCharge AllowanceOrCharge { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public AdditionalInformation[] AdditionalInformation { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public DateTimePeriod[] DateTimePeriod { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public SG44 SG44 { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public SG45 SG45 { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public SG46[] SG46 { get; set; }

        [DataMember]
        [ProtoMember(7)]
        public SG47 SG47 { get; set; }

        [DataMember]
        [ProtoMember(8)]
        public SG48[] SG48 { get; set; }
    }
}