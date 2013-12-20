using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class AdditionalInformation
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] CountryOfOriginNameCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public int[] DutyRegimeTypeCode { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string[] SpecialConditionCode { get; set; }
    }
}