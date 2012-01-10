using System;
using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class MonetaryAmountGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public char[] MonetaryAmountTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public char?[] MonetaryAmount { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public uint?[] CurrencyIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public long?[] CurrencyTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public DateTime?[] StatusDescriptionCode { get; set; }
    }
}