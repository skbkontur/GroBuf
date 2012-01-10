using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class SpecialServicesIdentification
    {
        [DataMember]
        [ProtoMember(1)]
        public bool[] SpecialServiceDescriptionCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public sbyte CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public bool? CodeListResponsibleAgencyCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public bool?[] SpecialServiceDescription { get; set; }
    }
}