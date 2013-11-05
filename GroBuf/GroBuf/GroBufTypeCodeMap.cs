using System;

namespace GroBuf
{
    public static class GroBufTypeCodeMap
    {
        public static GroBufTypeCode GetTypeCode(Type type)
        {
            switch(Type.GetTypeCode(type))
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
                //return GroBufTypeCode.DateTimeNew;
                return GroBufTypeCode.DateTimeOld;
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
        }
    }
}