using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Invoic
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