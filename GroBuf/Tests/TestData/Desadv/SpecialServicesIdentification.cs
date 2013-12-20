using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class SpecialServicesIdentification
    {
        [DataMember]
        [ProtoMember(1)]
        public int[] SpecialServiceDescriptionCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public sbyte[] CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public short[] CodeListResponsibleAgencyCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public ushort[] SpecialServiceDescription { get; set; }
    }
}