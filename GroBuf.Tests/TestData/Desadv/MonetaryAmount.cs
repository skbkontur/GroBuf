using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class MonetaryAmount
    {
        [DataMember]
        [ProtoMember(1)]
        public MonetaryAmountGroup MonetaryAmountGroup { get; set; }
    }
}