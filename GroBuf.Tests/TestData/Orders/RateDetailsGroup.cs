using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class RateDetailsGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public bool RateTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public float?[] UnitPriceBasisRate { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public ushort[] UnitPriceBasisValue { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public short?[] MeasurementUnitCode { get; set; }
    }
}