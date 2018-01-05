using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class Range
    {
        [DataMember]
        [ProtoMember(1)]
        public string MeasurementUnitCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public string RangeMinimumValue { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string RangeMaximumValue { get; set; }
    }
}