using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class RateDetailsGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] RateTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] UnitPriceBasisRate { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte[] UnitPriceBasisValue { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public byte[] MeasurementUnitCode { get; set; }
    }
}