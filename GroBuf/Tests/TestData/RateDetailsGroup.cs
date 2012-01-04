namespace SKBKontur.GroBuf.Tests.TestData
{
    public class RateDetailsGroup
    {
        public bool RateTypeCodeQualifier { get; set; }

        public float?[] UnitPriceBasisRate { get; set; }

        public ushort[] UnitPriceBasisValue { get; set; }

        public short?[] MeasurementUnitCode { get; set; }
    }
}