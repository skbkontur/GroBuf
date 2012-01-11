using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class QuantityDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public double? QuantityTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public float[] Quantity { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public float? MeasurementUnitCode { get; set; }
    }
}