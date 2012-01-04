using System;

namespace SKBKontur.GroBuf.Tests.TestData
{
    public class PercentageDetailsGroup
    {
        public uint[] PercentageTypeCodeQualifier { get; set; }

        public DateTime? Percentage { get; set; }

        public float PercentageBasisIdentificationCode { get; set; }

        public float? CodeListIdentificationCode { get; set; }

        public string CodeListResponsibleAgencyCode { get; set; }
    }
}