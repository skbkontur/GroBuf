namespace SKBKontur.GroBuf
{
    public enum GroBufTypeCode
    {
        [DataLength(1)]
        Empty = 0,

        [DataLength(-1)]
        Object = 1,

        [DataLength(-1)]
        Array = 2,

        [DataLength(1)]
        Int8 = 3,

        [DataLength(1)]
        UInt8 = 4,

        [DataLength(2)]
        Int16 = 5,

        [DataLength(2)]
        UInt16 = 6,

        [DataLength(4)]
        Int32 = 7,

        [DataLength(4)]
        UInt32 = 8,

        [DataLength(8)]
        Int64 = 9,

        [DataLength(8)]
        UInt64 = 10,

        [DataLength(4)]
        Single = 11,

        [DataLength(8)]
        Double = 12,

        [DataLength(-1)]
        Decimal = 13,

        [DataLength(-1)]
        String = 14,

        [DataLength(16)]
        Guid = 15,

        [DataLength(8)]
        Enum = 16
    }
}