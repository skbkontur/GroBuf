namespace SKBKontur.GroBuf.Tests.TestData
{
    public class AllowanceOrCharge
    {
        public byte AllowanceOrChargeCodeQualifier { get; set; }

        public AllowanceChargeInformation AllowanceChargeInformation { get; set; }

        public short SettlementMeansCode { get; set; }

        public ushort CalculationSequenceCode { get; set; }

        public SpecialServicesIdentification SpecialServicesIdentification { get; set; }
    }
}