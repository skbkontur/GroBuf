using System;
using System.Collections.Generic;
using System.Linq;

namespace GroBuf
{
    public static class GroBufHelpers
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
                return GroBufTypeCode.DateTime;
            case TypeCode.String:
                return GroBufTypeCode.String;
            default:
                if(type == typeof(Guid))
                    return GroBufTypeCode.Guid;
                if(type.IsEnum)
                    return GroBufTypeCode.Enum;
                if(type.IsArray || type == typeof(Array))
                    return GroBufTypeCode.Array;
                return GroBufTypeCode.Object;
            }
        }

        public static ulong[] CalcHashAndCheck(IEnumerable<string> strings)
        {
            var dict = new Dictionary<ulong, string>();
            foreach(var s in strings)
            {
                var hash = CalcHash(s);
                if(hash == 0)
                    throw new InvalidOperationException("Hash code of '" + s + "' equals to zero");
                if(dict.ContainsKey(hash))
                {
                    if(dict[hash] == s)
                        throw new InvalidOperationException("Duplicated string '" + s + "'");
                    throw new InvalidOperationException("Hash code collision: strings '" + s + "' and '" + dict[hash] + "' have the same hash code = '" + hash + "'");
                }
                dict.Add(hash, s);
            }
            return dict.Keys.ToArray();
        }

        public static ulong CalcHash(string str)
        {
            if(randTable.Count < str.Length * 2)
                InitRandTable(str.Length * 2);
            ulong result = 0;
            for(int i = 0; i < str.Length; ++i)
            {
                result ^= randTable[2 * i][str[i] & 0xFF];
                result ^= randTable[2 * i + 1][(str[i] >> 8) & 0xFF];
            }
            return result;
        }

        public static readonly int[] Lengths = BuildLengths();

        private static int[] BuildLengths()
        {
            var lengths = new int[256];
            var type = typeof(GroBufTypeCode);
            var fields = type.GetFields();
            foreach(var field in fields)
            {
                if(field.FieldType != type) continue;
                var attribute = (DataLengthAttribute)field.GetCustomAttributes(typeof(DataLengthAttribute), false).SingleOrDefault();
                if(attribute == null) throw new InvalidOperationException(string.Format("Data length of '{0}.{1}' must be specified", type, field));
                var length = attribute.Length;
                if(length < 0 && length != -1)
                    throw new InvalidOperationException("Data length must be either -1 or greater 0");
                lengths[(int)field.GetValue(dummy)] = length;
            }
            return lengths;
        }

        private static void InitRandTable(int count)
        {
            while(randTable.Count < count)
            {
                var arr = new ulong[256];
                for(int i = 0; i < arr.Length; ++i)
                    arr[i] = ((ulong)(random.Next() & 0xFFFFFF)) | (((ulong)(random.Next() & 0xFFFFFF)) << 24) | (((ulong)(random.Next() & 0xFFFFFF)) << 48);
                randTable.Add(arr);
            }
        }

        private static readonly object dummy = new object();

        private static readonly GroBufRandom random = new GroBufRandom(314159265);
        private static readonly List<ulong[]> randTable = new List<ulong[]>();
    }
}