using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class AllowanceChargeInformation
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] AllowanceOrChargeIdentifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] AllowanceOrChargeIdentificationCode { get; set; }
    }
}