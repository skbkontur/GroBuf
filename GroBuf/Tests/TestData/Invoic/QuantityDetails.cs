using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class QuantityDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public string QuantityTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public string Quantity { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string MeasurementUnitCode { get; set; }
    }
}