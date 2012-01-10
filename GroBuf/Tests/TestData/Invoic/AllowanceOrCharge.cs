using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class AllowanceOrCharge
    {
        [DataMember]
        [ProtoMember(1)]
        public string AllowanceOrChargeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public AllowanceChargeInformation AllowanceChargeInformation { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string SettlementMeansCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public string CalculationSequenceCode { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public SpecialServicesIdentification SpecialServicesIdentification { get; set; }
    }
}