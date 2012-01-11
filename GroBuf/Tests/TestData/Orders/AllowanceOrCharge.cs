using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class AllowanceOrCharge
    {
        [DataMember]
        [ProtoMember(1)]
        public byte AllowanceOrChargeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public AllowanceChargeInformation AllowanceChargeInformation { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public short SettlementMeansCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public ushort CalculationSequenceCode { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public SpecialServicesIdentification SpecialServicesIdentification { get; set; }
    }
}