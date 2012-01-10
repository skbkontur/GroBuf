using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Invoic
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
        public string StatusDescriptionCode { get; set; }
    }
}