using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class AdditionalInformation
    {
        [DataMember]
        [ProtoMember(1)]
        public string CountryOfOriginNameCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public string DutyRegimeTypeCode { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string[] SpecialConditionCode { get; set; }
    }
}