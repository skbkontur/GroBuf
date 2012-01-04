using System;

namespace SKBKontur.GroBuf.Tests.TestData
{
    public class MonetaryAmountGroup
    {
        public char[] MonetaryAmountTypeCodeQualifier { get; set; }

        public char?[] MonetaryAmount { get; set; }

        public uint?[] CurrencyIdentificationCode { get; set; }

        public long?[] CurrencyTypeCodeQualifier { get; set; }

        public DateTime?[] StatusDescriptionCode { get; set; }
    }
}