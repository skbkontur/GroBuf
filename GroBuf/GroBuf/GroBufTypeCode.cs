namespace GroBuf
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
        Enum = 16,

        [DataLength(1)]
        Boolean = 17,

        [DataLength(8)]
        DateTime = 18,

        [DataLength(-1)]
        Int8Array = 19,

        [DataLength(-1)]
        UInt8Array = 20,

        [DataLength(-1)]
        Int16Array = 21,

        [DataLength(-1)]
        UInt16Array = 22,

        [DataLength(-1)]
        Int32Array = 23,

        [DataLength(-1)]
        UInt32Array = 24,

        [DataLength(-1)]
        Int64Array = 25,

        [DataLength(-1)]
        UInt64Array = 26,

        [DataLength(-1)]
        SingleArray = 27,

        [DataLength(-1)]
        DoubleArray = 28,

        [DataLength(-1)]
        BooleanArray = 29,

        [DataLength(-1)]
        CustomData = 255
    }
}