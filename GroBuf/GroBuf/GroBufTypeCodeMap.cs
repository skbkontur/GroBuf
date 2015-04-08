using System;
using System.Collections;

namespace GroBuf
{
    public static class GroBufTypeCodeMap
    {
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

        private static GroBufTypeCode[] BuildItemToArrayMap()
        {
            var result = new GroBufTypeCode[256];
            for(int i = 0; i < 256; ++i)
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
            if(result != GroBufTypeCode.Object)
                return result;
            if (type.IsEnum)
                return GroBufTypeCode.Enum;
            if (type.IsArray && type.GetArrayRank() == 1)
                return mapItemToArray[(int)GetTypeCode(type.GetElementType())];
            if(type == typeof(Guid)) return GroBufTypeCode.Guid;
            if(type == typeof(Hashtable)) return GroBufTypeCode.Dictionary;
            return GroBufTypeCode.Object;
/*

            switch (Type.GetTypeCode(type))
            {
            case TypeCode.Boolean:
                return GroBufTypeCode.Boolean;
            case TypeCode.Char:
                return GroBufTypeCode.UInt16;
            case TypeCode.SByte:
                return GroBufTypeCode.Int8;
            case TypeCode.Byte:
                return GroBufTypeCode.UInt8;
            case TypeCode.Int16:
                return GroBufTypeCode.Int16;
            case TypeCode.UInt16:
                return GroBufTypeCode.UInt16;
            case TypeCode.Int32:
                return GroBufTypeCode.Int32;
            case TypeCode.UInt32:
                return GroBufTypeCode.UInt32;
            case TypeCode.Int64:
                return GroBufTypeCode.Int64;
            case TypeCode.UInt64:
                return GroBufTypeCode.UInt64;
            case TypeCode.Single:
                return GroBufTypeCode.Single;
            case TypeCode.Double:
                return GroBufTypeCode.Double;
            case TypeCode.Decimal:
                return GroBufTypeCode.Decimal;
            case TypeCode.DateTime:
                return GroBufTypeCode.DateTimeNew;
            case TypeCode.String:
                return GroBufTypeCode.String;
            default:
                if(type == typeof(Guid))
                    return GroBufTypeCode.Guid;
                if(type.IsEnum)
                    return GroBufTypeCode.Enum;
                if(type.IsArray || type == typeof(Array))
                {
                    var elementTypeCode = GetTypeCode(type.GetElementType() ?? typeof(object));
                    switch(elementTypeCode)
                    {
                    case GroBufTypeCode.Int8:
                        return GroBufTypeCode.Int8Array;
                    case GroBufTypeCode.UInt8:
                        return GroBufTypeCode.UInt8Array;
                    case GroBufTypeCode.Int16:
                        return GroBufTypeCode.Int16Array;
                    case GroBufTypeCode.UInt16:
                        return GroBufTypeCode.UInt16Array;
                    case GroBufTypeCode.Int32:
                        return GroBufTypeCode.Int32Array;
                    case GroBufTypeCode.UInt32:
                        return GroBufTypeCode.UInt32Array;
                    case GroBufTypeCode.Int64:
                        return GroBufTypeCode.Int64Array;
                    case GroBufTypeCode.UInt64:
                        return GroBufTypeCode.UInt64Array;
                    case GroBufTypeCode.Single:
                        return GroBufTypeCode.SingleArray;
                    case GroBufTypeCode.Double:
                        return GroBufTypeCode.DoubleArray;
                    case GroBufTypeCode.Boolean:
                        return GroBufTypeCode.BooleanArray;
                    default:
                        return GroBufTypeCode.Array;
                    }
                }
                return GroBufTypeCode.Object;
            }
*/
        }
    }
}