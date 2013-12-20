using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class QuantityDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] QuantityTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] Quantity { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte[] MeasurementUnitCode { get; set; }
    }
}