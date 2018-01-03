using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class PercentageDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public PercentageDetailsGroup PercentageDetailsGroup { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] StatusDescriptionCode { get; set; }
    }
}