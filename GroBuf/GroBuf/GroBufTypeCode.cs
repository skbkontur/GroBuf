using System;
using System.Collections;

namespace GroBuf
{
    public enum GroBufTypeCode
    {
        [DataLength(1)]
        Empty = 0,

        [DataLength(-1)]
        Object = 1,

        [DataLength(-1), LeafType(typeof(Array))]
        Array = 2,

        [DataLength(1), LeafType(typeof(sbyte))]
        Int8 = 3,

        [DataLength(1), LeafType(typeof(byte))]
        UInt8 = 4,

        [DataLength(2), LeafType(typeof(short))]
        Int16 = 5,

        [DataLength(2), LeafType(typeof(ushort))]
        UInt16 = 6,

        [DataLength(4), LeafType(typeof(int))]
        Int32 = 7,

        [DataLength(4), LeafType(typeof(uint))]
        UInt32 = 8,

        [DataLength(8), LeafType(typeof(long))]
        Int64 = 9,

        [DataLength(8), LeafType(typeof(ulong))]
        UInt64 = 10,

        [DataLength(4), LeafType(typeof(float))]
        Single = 11,

        [DataLength(8), LeafType(typeof(double))]
        Double = 12,

        [DataLength(16), LeafType(typeof(decimal))]
        Decimal = 13,

        [DataLength(-1), LeafType(typeof(string))]
        String = 14,

        [DataLength(16), LeafType(typeof(Guid))]
        Guid = 15,

        [DataLength(8)]
        Enum = 16,

        [DataLength(1), LeafType(typeof(bool))]
        Boolean = 17,

        [DataLength(-2)]
        DateTimeOld = 18,

        [DataLength(-1), LeafType(typeof(sbyte[]))]
        Int8Array = 19,

        [DataLength(-1), LeafType(typeof(byte[]))]
        UInt8Array = 20,

        [DataLength(-1), LeafType(typeof(short[]))]
        Int16Array = 21,

        [DataLength(-1), LeafType(typeof(ushort[]))]
        UInt16Array = 22,

        [DataLength(-1), LeafType(typeof(int[]))]
        Int32Array = 23,

        [DataLength(-1), LeafType(typeof(uint[]))]
        UInt32Array = 24,

        [DataLength(-1), LeafType(typeof(long[]))]
        Int64Array = 25,

        [DataLength(-1), LeafType(typeof(ulong[]))]
        UInt64Array = 26,

        [DataLength(-1), LeafType(typeof(float[]))]
        SingleArray = 27,

        [DataLength(-1), LeafType(typeof(double[]))]
        DoubleArray = 28,

        [DataLength(-1), LeafType(typeof(bool[]))]
        BooleanArray = 29,

        [DataLength(-1), LeafType(typeof(Hashtable))]
        Dictionary = 30,

        [DataLength(8), LeafType(typeof(DateTime))]
        DateTimeNew = 31,

        [DataLength(4)]
        Reference = 32,

        [DataLength(-1)]
        CustomData = 255
    }
}