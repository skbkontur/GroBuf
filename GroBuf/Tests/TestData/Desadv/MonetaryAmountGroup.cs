using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class MonetaryAmountGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] MonetaryAmountTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] MonetaryAmount { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte[] CurrencyIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public byte[] CurrencyTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public byte[] StatusDescriptionCode { get; set; }
    }
}