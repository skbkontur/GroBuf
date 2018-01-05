using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class RateDetailsGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public string RateTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public string UnitPriceBasisRate { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string UnitPriceBasisValue { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public string MeasurementUnitCode { get; set; }
    }
}