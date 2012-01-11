using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class MonetaryAmountGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public string MonetaryAmountTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public string MonetaryAmount { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string CurrencyIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public string CurrencyTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public string StatusDescriptionCode { get; set; }
    }
}