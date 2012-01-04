namespace SKBKontur.GroBuf.Tests.TestData
{
    public class DutyTaxFeeDetails
    {
        public int[] DutyTaxFeeFunctionCodeQualifier { get; set; }

        public DutyTaxFeeType DutyTaxFeeType { get; set; }

        public DutyTaxFeeAccountDetail DutyTaxFeeAccountDetail { get; set; }

        public ulong?[] DutyTaxFeeAssessmentBasisValue { get; set; }

        public DutyTaxFeeDetail DutyTaxFeeDetail { get; set; }

        public byte[] DutyTaxFeeCategoryCode { get; set; }

        public sbyte[] PartyTaxIdentifier { get; set; }

        public sbyte?[] CalculationSequenceCode { get; set; }
    }
}