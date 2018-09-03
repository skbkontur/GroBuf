using System;
using System.Collections;

namespace GroBuf
{
    public static class GroBufTypeCodeMap
    {
        private static GroBufTypeCode[] BuildItemToArrayMap()
        {
            var result = new GroBufTypeCode[256];
            for (int i = 0; i < 256; ++i)
                result[i] = GroBufTypeCode.Array;
            result[(int)GroBufTypeCode.Int8] = GroBufTypeCode.Int8Array;
            result[(int)GroBufTypeCode.UInt8] = GroBufTypeCode.UInt8Array;
            result[(int)GroBufTypeCode.Int16] = GroBufTypeCode.Int16Array;
            result[(int)GroBufTypeCode.UInt16] = GroBufTypeCode.UInt16Array;
            result[(int)GroBufTypeCode.Int32] = GroBufTypeCode.Int32Array;
            result[(int)GroBufTypeCode.UInt32] = GroBufTypeCode.UInt32Array;
            result[(int)GroBufTypeCode.Int64] = GroBufTypeCode.Int64Array;
            result[(int)GroBufTypeCode.UInt64] = GroBufTypeCode.UInt64Array;
            result[(int)GroBufTypeCode.Single] = GroBufTypeCode.SingleArray;
            result[(int)GroBufTypeCode.Double] = GroBufTypeCode.DoubleArray;
            result[(int)GroBufTypeCode.Boolean] = GroBufTypeCode.BooleanArray;
            return result;
        }

        public static GroBufTypeCode GetTypeCode(Type type)
        {
            var result = map[(int)Type.GetTypeCode(type)];
            if (result != GroBufTypeCode.Object)
                return result;
            if (type.IsEnum)
                return GroBufTypeCode.Enum;
            if (type.IsArray && type.GetArrayRank() == 1)
                return mapItemToArray[(int)GetTypeCode(type.GetElementType())];
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ArraySegment<>))
                return mapItemToArray[(int)GetTypeCode(type.GetGenericArguments()[0])];
            if (type == typeof(Guid)) return GroBufTypeCode.Guid;
            if (type == typeof(Hashtable)) return GroBufTypeCode.Dictionary;
            return GroBufTypeCode.Object;
        }

        private static readonly GroBufTypeCode[] map = new[]
            {
                GroBufTypeCode.Empty,
                GroBufTypeCode.Object,
                GroBufTypeCode.Empty,
                GroBufTypeCode.Boolean,
                GroBufTypeCode.UInt16,
                GroBufTypeCode.Int8,
                GroBufTypeCode.UInt8,
                GroBufTypeCode.Int16,
                GroBufTypeCode.UInt16,
                GroBufTypeCode.Int32,
                GroBufTypeCode.UInt32,
                GroBufTypeCode.Int64,
                GroBufTypeCode.UInt64,
                GroBufTypeCode.Single,
                GroBufTypeCode.Double,
                GroBufTypeCode.Decimal,
                GroBufTypeCode.DateTimeNew,
                GroBufTypeCode.Empty,
                GroBufTypeCode.String,
            };

        private static readonly GroBufTypeCode[] mapItemToArray = BuildItemToArrayMap();
    }
}