using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class AllowanceOrCharge
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] AllowanceOrChargeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public AllowanceChargeInformation AllowanceChargeInformation { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte[] SettlementMeansCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public byte[] CalculationSequenceCode { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public SpecialServicesIdentification SpecialServicesIdentification { get; set; }
    }
}