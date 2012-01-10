using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class Range
    {
        [DataMember]
        [ProtoMember(1)]
        public double[] MeasurementUnitCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public double?[] RangeMinimumValue { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public float RangeMaximumValue { get; set; }
    }
}