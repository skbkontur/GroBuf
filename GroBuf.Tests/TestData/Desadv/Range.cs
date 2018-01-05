using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class Range
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] MeasurementUnitCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] RangeMinimumValue { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte[] RangeMaximumValue { get; set; }
    }
}